<?php
require_once 'lib/bootstrap.php';
exigir_login();
if (!pode('Administrador', 'Recepcao')) { flash('warning', 'Acesso restrito ao faturamento.'); redirecionar('index.php'); }

$api = api();
$filtro = $_GET['status'] ?? '';
$resumo = $api->get('faturamentos/resumo')->data ?? ['totalPendente' => 0, 'totalRecebidoMes' => 0, 'quantidadeEmAuditoria' => 0];
$r = $api->get('faturamentos', ['status' => $filtro]);
if (!$r->success) flash_api($r);
$pacientes = $api->get('pacientes')->data ?? [];

// Consolidação de gastos de um paciente (RN-FAT-02)
$idConsolidar = (int) ($_GET['consolidar'] ?? 0);
$consolidacao = $idConsolidar ? $api->get("pacientes/$idConsolidar/gastos") : null;
require 'views/header.php';
?>
<div class="d-flex justify-content-between align-items-center pt-3 pb-2 mb-3 border-bottom">
    <h1 class="h2">Faturamento Hospitalar</h1>
    <div>
        <button type="button" class="btn btn-sm btn-outline-primary me-2" onclick="window.print()"><i class="fas fa-file-export me-1"></i> Exportar Relatório</button>
        <button type="button" class="btn btn-primary" data-bs-toggle="modal" data-bs-target="#modalNovaGuia"><i class="fas fa-plus me-1"></i> Nova Guia</button>
    </div>
</div>

<div class="row mb-4">
    <div class="col-md-4"><div class="card border-start border-4 border-primary shadow-sm"><div class="card-body">
        <h6 class="card-subtitle mb-2 text-muted small text-uppercase">Total Pendente</h6><h3 class="card-title mb-0"><?= formatarMoeda($resumo['totalPendente']) ?></h3>
    </div></div></div>
    <div class="col-md-4"><div class="card border-start border-4 border-success shadow-sm"><div class="card-body">
        <h6 class="card-subtitle mb-2 text-muted small text-uppercase">Recebido (<?= date('m/Y') ?>)</h6><h3 class="card-title mb-0"><?= formatarMoeda($resumo['totalRecebidoMes']) ?></h3>
    </div></div></div>
    <div class="col-md-4"><div class="card border-start border-4 border-warning shadow-sm"><div class="card-body">
        <h6 class="card-subtitle mb-2 text-muted small text-uppercase">Aguardando Auditoria</h6><h3 class="card-title mb-0"><?= (int) $resumo['quantidadeEmAuditoria'] ?> Guias</h3>
    </div></div></div>
</div>

<div class="card shadow-sm">
    <div class="card-body">
        <form class="row g-2 align-items-end" method="GET">
            <div class="col-md-6"><label class="form-label small">Consolidar gastos do paciente</label>
                <select class="form-select" name="consolidar">
                    <option value="">Selecione...</option>
                    <?php foreach ($pacientes as $p): ?><option value="<?= (int) $p['id'] ?>" <?= $p['id'] === $idConsolidar ? 'selected' : '' ?>><?= e($p['nome']) ?></option><?php endforeach; ?>
                </select>
            </div>
            <div class="col-md-2"><button class="btn btn-outline-primary w-100">Calcular</button></div>
        </form>
        <?php if ($consolidacao && $consolidacao->success): $g = $consolidacao->data; ?>
        <div class="mt-3 p-3 bg-light rounded small">
            Insumos: <strong><?= formatarMoeda($g['totalInsumos']) ?></strong> ·
            Exames: <?= (int) $g['quantidadeExames'] ?> × R$ 100,00 = <strong><?= formatarMoeda($g['totalExames']) ?></strong> ·
            Prescrições: <?= (int) $g['quantidadePrescricoes'] ?> × R$ 50,00 = <strong><?= formatarMoeda($g['totalPrescricoes']) ?></strong>
            <span class="fs-6 ms-2">Total: <strong><?= formatarMoeda($g['valorTotal']) ?></strong></span>
            <?php if ($g['valorTotal'] > 0): ?>
            <form method="POST" action="processar_faturamento.php" class="d-inline ms-3">
                <input type="hidden" name="acao" value="nova_guia"><input type="hidden" name="id_paciente" value="<?= $idConsolidar ?>">
                <button class="btn btn-sm btn-success">Emitir guia com este valor</button>
            </form>
            <?php endif; ?>
        </div>
        <?php elseif ($consolidacao): ?><div class="alert alert-danger mt-3 mb-0"><?= e($consolidacao->message) ?></div><?php endif; ?>
    </div>
</div>

<div class="card shadow-sm">
    <div class="card-header bg-white py-3 d-flex justify-content-between">
        <span><i class="fas fa-file-invoice me-1 text-primary"></i> Guias de Faturamento</span>
        <div class="btn-group btn-group-sm">
            <?php foreach (['' => 'Todas', 'Pendente' => 'Pendentes', 'Auditoria' => 'Auditoria', 'Pago' => 'Pagas', 'Cancelado' => 'Canceladas'] as $v => $rot): ?>
                <a href="faturamento.php?status=<?= urlencode($v) ?>" class="btn <?= $filtro === $v ? 'btn-secondary' : 'btn-outline-secondary' ?>"><?= $rot ?></a>
            <?php endforeach; ?>
        </div>
    </div>
    <div class="card-body">
        <div class="table-responsive">
            <table class="table table-hover align-middle">
                <thead class="table-light"><tr><th>ID</th><th>Paciente</th><th>Data Emissão</th><th>Valor Total</th><th>Status</th><th>Ações</th></tr></thead>
                <tbody>
                <?php foreach ($r->data ?? [] as $f):
                    $badge = ['Pago' => 'bg-success', 'Pendente' => 'bg-primary', 'Auditoria' => 'bg-info', 'Cancelado' => 'bg-secondary'][$f['status']] ?? 'bg-warning'; ?>
                    <tr>
                        <td>#<?= (int) $f['id'] ?></td>
                        <td><strong><?= e($f['pacienteNome']) ?></strong></td>
                        <td><?= formatarData($f['dataFaturamento']) ?></td>
                        <td><?= formatarMoeda($f['valorTotal']) ?></td>
                        <td><span class="badge <?= $badge ?>"><?= e($f['status']) ?></span></td>
                        <td>
                            <div class="d-flex gap-1">
                            <?php
                            $botoes = match ($f['status']) {
                                'Pendente' => ['pagar' => ['Baixar', 'btn-success'], 'auditoria' => ['Auditoria', 'btn-outline-info']],
                                'Auditoria' => ['aprovar' => ['Aprovar', 'btn-outline-primary']],
                                default => [],
                            };
                            foreach ($botoes as $acao => [$rotulo, $cls]): ?>
                                <form method="POST" action="processar_faturamento.php">
                                    <input type="hidden" name="acao" value="<?= $acao ?>"><input type="hidden" name="id" value="<?= (int) $f['id'] ?>">
                                    <button class="btn btn-sm <?= $cls ?>"><?= $rotulo ?></button>
                                </form>
                            <?php endforeach; ?>
                            <?php if (in_array($f['status'], ['Pendente', 'Auditoria'], true)): ?>
                                <form method="POST" action="processar_faturamento.php" onsubmit="const m = prompt('Motivo do cancelamento:'); if (!m) return false; this.motivo.value = m;">
                                    <input type="hidden" name="acao" value="cancelar"><input type="hidden" name="id" value="<?= (int) $f['id'] ?>"><input type="hidden" name="motivo">
                                    <button class="btn btn-sm btn-outline-danger">Cancelar</button>
                                </form>
                            <?php endif; ?>
                            </div>
                        </td>
                    </tr>
                <?php endforeach; ?>
                <?php if (empty($r->data)): ?><tr><td colspan="6" class="text-center py-5 text-muted">Nenhuma guia de faturamento encontrada.</td></tr><?php endif; ?>
                </tbody>
            </table>
        </div>
    </div>
</div>

<div class="modal fade" id="modalNovaGuia" tabindex="-1"><div class="modal-dialog"><div class="modal-content">
    <form action="processar_faturamento.php" method="POST">
        <input type="hidden" name="acao" value="nova_guia">
        <div class="modal-header"><h5 class="modal-title">Emitir Nova Guia</h5><button type="button" class="btn-close" data-bs-dismiss="modal"></button></div>
        <div class="modal-body">
            <div class="mb-3"><label class="form-label">Paciente</label>
                <select class="form-select" name="id_paciente" required>
                    <option value="">Selecione...</option>
                    <?php foreach ($pacientes as $p): ?><option value="<?= (int) $p['id'] ?>"><?= e($p['nome']) ?></option><?php endforeach; ?>
                </select>
            </div>
            <div class="mb-3"><label class="form-label">Valor Total (R$)</label>
                <input type="number" step="0.01" min="0.01" class="form-control" name="valor" placeholder="Em branco = usar consolidação de gastos">
                <small class="text-muted">Guias a partir de R$ 10.000,00 vão automaticamente para auditoria.</small>
            </div>
        </div>
        <div class="modal-footer"><button type="submit" class="btn btn-primary">Gerar Guia</button></div>
    </form>
</div></div></div>
<?php require 'views/footer.php'; ?>
