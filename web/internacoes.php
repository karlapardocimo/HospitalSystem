<?php
require_once 'lib/bootstrap.php';
exigir_login();

$todas = isset($_GET['todas']);
$api = api();
$r = $api->get('internacoes', ['somenteAtivas' => $todas ? 'false' : 'true']);
if (!$r->success) flash_api($r);
$podeOperar = pode('Administrador', 'Medico', 'Enfermagem');
$leitosLivres = $podeOperar ? ($api->get('leitos', ['status' => 'Disponível'])->data ?? []) : [];
$pacientes = $podeOperar ? ($api->get('pacientes')->data ?? []) : [];
$leitoSelecionado = (int) ($_GET['leito'] ?? 0);
require 'views/header.php';
?>
<div class="d-flex justify-content-between align-items-center pt-3 pb-2 mb-3 border-bottom">
    <h1 class="h2">Internações</h1>
    <div class="btn-group">
        <a href="internacoes.php" class="btn btn-sm <?= !$todas ? 'btn-secondary' : 'btn-outline-secondary' ?>">Ativas</a>
        <a href="internacoes.php?todas=1" class="btn btn-sm <?= $todas ? 'btn-secondary' : 'btn-outline-secondary' ?>">Todas</a>
    </div>
</div>

<div class="row g-4">
    <?php if ($podeOperar): ?>
    <div class="col-lg-4">
        <div class="card">
            <div class="card-header">Internar Paciente</div>
            <div class="card-body">
                <form method="POST" action="processar_internacoes.php">
                    <input type="hidden" name="acao" value="internar">
                    <div class="mb-3"><label class="form-label">Paciente</label>
                        <select class="form-select" name="id_paciente" required>
                            <option value="">Selecione...</option>
                            <?php foreach ($pacientes as $p): ?><option value="<?= (int) $p['id'] ?>"><?= e($p['nome']) ?></option><?php endforeach; ?>
                        </select>
                    </div>
                    <div class="mb-3"><label class="form-label">Leito disponível</label>
                        <select class="form-select" name="id_leito" required>
                            <option value="">Selecione...</option>
                            <?php foreach ($leitosLivres as $l): ?><option value="<?= (int) $l['id'] ?>" <?= $l['id'] === $leitoSelecionado ? 'selected' : '' ?>><?= e($l['numero']) ?> - <?= e($l['ala']) ?></option><?php endforeach; ?>
                        </select>
                        <?php if (!$leitosLivres): ?><small class="text-danger">Nenhum leito disponível.</small><?php endif; ?>
                    </div>
                    <button class="btn btn-primary w-100"><i class="fas fa-procedures me-1"></i> Internar</button>
                </form>
            </div>
        </div>
    </div>
    <?php endif; ?>

    <div class="<?= $podeOperar ? 'col-lg-8' : 'col-12' ?>">
        <div class="card">
            <div class="card-header"><?= e($r->message) ?></div>
            <div class="card-body p-0">
                <table class="table table-hover align-middle mb-0">
                    <thead class="table-light"><tr><th class="ps-3">Paciente</th><th>Leito</th><th>Entrada</th><th>Alta</th><th>Status</th><th class="text-end pe-3">Ações</th></tr></thead>
                    <tbody>
                    <?php foreach ($r->data ?? [] as $i): ?>
                        <tr>
                            <td class="ps-3"><a href="prontuario.php?id=<?= (int) $i['idPaciente'] ?>"><strong><?= e($i['pacienteNome']) ?></strong></a></td>
                            <td><?= e($i['leitoNumero']) ?> <small class="text-muted"><?= e($i['ala']) ?></small></td>
                            <td><?= formatarDataHora($i['dataEntrada']) ?></td>
                            <td><?= formatarDataHora($i['dataAlta']) ?></td>
                            <td><span class="badge bg-<?= $i['status'] === 'Ativa' ? 'primary' : 'secondary' ?>"><?= e($i['status']) ?></span></td>
                            <td class="text-end pe-3">
                                <?php if ($i['status'] === 'Ativa' && $podeOperar): ?>
                                <form method="POST" action="processar_internacoes.php" class="d-inline-flex gap-1">
                                    <input type="hidden" name="acao" value="transferir"><input type="hidden" name="id" value="<?= (int) $i['id'] ?>">
                                    <select name="id_novo_leito" class="form-select form-select-sm" required>
                                        <option value="">Transferir p/...</option>
                                        <?php foreach ($leitosLivres as $l): ?><option value="<?= (int) $l['id'] ?>"><?= e($l['numero']) ?> - <?= e($l['ala']) ?></option><?php endforeach; ?>
                                    </select>
                                    <button class="btn btn-sm btn-outline-primary" title="Transferir"><i class="fas fa-exchange-alt"></i></button>
                                </form>
                                <?php if (pode('Administrador', 'Medico')): ?>
                                <form method="POST" action="processar_internacoes.php" class="d-inline" onsubmit="return confirm('Registrar alta? O leito irá para higienização.')">
                                    <input type="hidden" name="acao" value="alta"><input type="hidden" name="id" value="<?= (int) $i['id'] ?>">
                                    <button class="btn btn-sm btn-success">Alta</button>
                                </form>
                                <?php endif; ?>
                                <?php endif; ?>
                            </td>
                        </tr>
                    <?php endforeach; ?>
                    <?php if (empty($r->data)): ?><tr><td colspan="6" class="text-center py-4 text-muted">Nenhuma internação.</td></tr><?php endif; ?>
                    </tbody>
                </table>
            </div>
        </div>
    </div>
</div>
<?php require 'views/footer.php'; ?>
