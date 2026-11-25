<?php
require_once 'lib/bootstrap.php';
exigir_login();
$r = api()->get('insumos');
if (!$r->success) flash_api($r);
$podeOperar = pode('Administrador', 'Enfermagem');
$leitosOcupados = $podeOperar ? (api()->get('leitos', ['status' => 'Ocupado'])->data ?? []) : [];
require 'views/header.php';
?>
<div class="d-flex justify-content-between align-items-center pt-3 pb-2 mb-3 border-bottom">
    <h1 class="h2">Estoque de Insumos Hospitalares</h1>
    <?php if ($podeOperar): ?>
    <button type="button" class="btn btn-primary" data-bs-toggle="modal" data-bs-target="#modalNovoItem"><i class="fas fa-plus me-1"></i> Novo Item</button>
    <?php endif; ?>
</div>
<div class="card">
    <div class="card-header bg-white"><i class="fas fa-boxes me-1 text-primary"></i> <?= e($r->message) ?></div>
    <div class="card-body">
        <div class="table-responsive">
            <table class="table table-hover align-middle">
                <thead><tr><th>ID</th><th>Insumo</th><th>Quantidade</th><th>Valor Unitário</th><th>Status</th><th>Ações</th></tr></thead>
                <tbody>
                <?php foreach ($r->data ?? [] as $i): ?>
                    <tr>
                        <td><?= (int) $i['id'] ?></td>
                        <td><strong><?= e($i['nome']) ?></strong></td>
                        <td><?= (int) $i['quantidadeEstoque'] ?></td>
                        <td><?= formatarMoeda($i['valorUnitario']) ?></td>
                        <td><span class="badge bg-<?= $i['estoqueCritico'] ? 'danger' : 'success' ?>"><?= $i['estoqueCritico'] ? 'Crítico' : 'Normal' ?></span></td>
                        <td>
                            <?php if ($podeOperar): ?>
                            <button class="btn btn-sm btn-outline-secondary" onclick="abrir('modalAjuste', <?= (int) $i['id'] ?>, '<?= e(addslashes($i['nome'])) ?>')"><i class="fas fa-sliders-h"></i> Ajustar</button>
                            <button class="btn btn-sm btn-outline-primary" onclick="abrir('modalUso', <?= (int) $i['id'] ?>, '<?= e(addslashes($i['nome'])) ?>')"><i class="fas fa-bed"></i> Usar em leito</button>
                            <?php endif; ?>
                        </td>
                    </tr>
                <?php endforeach; ?>
                <?php if (empty($r->data)): ?><tr><td colspan="6" class="text-center">Nenhum insumo cadastrado.</td></tr><?php endif; ?>
                </tbody>
            </table>
        </div>
    </div>
</div>

<div class="modal fade" id="modalNovoItem" tabindex="-1"><div class="modal-dialog"><div class="modal-content">
    <form action="processar_estoque.php" method="POST">
        <input type="hidden" name="acao" value="novo">
        <div class="modal-header"><h5 class="modal-title">Cadastrar Novo Insumo</h5><button type="button" class="btn-close" data-bs-dismiss="modal"></button></div>
        <div class="modal-body">
            <div class="mb-3"><label class="form-label">Nome do Insumo</label><input type="text" class="form-control" name="nome" required></div>
            <div class="row">
                <div class="col-md-6 mb-3"><label class="form-label">Quantidade Inicial</label><input type="number" min="0" class="form-control" name="quantidade" required></div>
                <div class="col-md-6 mb-3"><label class="form-label">Valor Unitário (R$)</label><input type="number" step="0.01" min="0.01" class="form-control" name="valor" required></div>
            </div>
        </div>
        <div class="modal-footer"><button type="submit" class="btn btn-primary">Salvar Item</button></div>
    </form>
</div></div></div>

<div class="modal fade" id="modalAjuste" tabindex="-1"><div class="modal-dialog"><div class="modal-content">
    <form action="processar_estoque.php" method="POST">
        <input type="hidden" name="acao" value="ajuste"><input type="hidden" name="id_insumo" class="campo-id">
        <div class="modal-header"><h5 class="modal-title">Ajustar Estoque: <span class="campo-nome"></span></h5><button type="button" class="btn-close" data-bs-dismiss="modal"></button></div>
        <div class="modal-body">
            <div class="mb-3"><label class="form-label">Operação</label>
                <select class="form-select" name="operacao"><option value="entrada">Adicionar (Entrada / Compra)</option><option value="saida">Remover (Saída / Perda)</option></select>
            </div>
            <div class="mb-3"><label class="form-label">Quantidade</label><input type="number" min="1" class="form-control" name="quantidade" required></div>
        </div>
        <div class="modal-footer"><button type="submit" class="btn btn-primary">Confirmar Ajuste</button></div>
    </form>
</div></div></div>

<div class="modal fade" id="modalUso" tabindex="-1"><div class="modal-dialog"><div class="modal-content">
    <form action="processar_estoque.php" method="POST">
        <input type="hidden" name="acao" value="uso"><input type="hidden" name="id_insumo" class="campo-id">
        <div class="modal-header"><h5 class="modal-title">Usar <span class="campo-nome"></span> em paciente internado</h5><button type="button" class="btn-close" data-bs-dismiss="modal"></button></div>
        <div class="modal-body">
            <div class="mb-3"><label class="form-label">Leito ocupado</label>
                <select class="form-select" name="id_leito" required>
                    <option value="">Selecione...</option>
                    <?php foreach ($leitosOcupados as $l): ?><option value="<?= (int) $l['id'] ?>"><?= e($l['numero']) ?> - <?= e($l['ala']) ?> (<?= e($l['pacienteAtual']) ?>)</option><?php endforeach; ?>
                </select>
            </div>
            <div class="mb-3"><label class="form-label">Quantidade</label><input type="number" min="1" class="form-control" name="quantidade" required></div>
            <small class="text-muted">O valor entra na consolidação de gastos do paciente (faturamento).</small>
        </div>
        <div class="modal-footer"><button type="submit" class="btn btn-primary">Lançar uso</button></div>
    </form>
</div></div></div>

<script>
function abrir(modal, id, nome) {
    const el = document.getElementById(modal);
    el.querySelector('.campo-id').value = id;
    el.querySelector('.campo-nome').innerText = nome;
    new bootstrap.Modal(el).show();
}
</script>
<?php require 'views/footer.php'; ?>
