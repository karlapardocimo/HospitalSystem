<?php
require_once 'lib/bootstrap.php';
exigir_login();
$r = api()->get('salas');   // GET /api/salas (com cache na API)
if (!$r->success) flash_api($r);
require 'views/header.php';
?>
<div class="d-flex justify-content-between align-items-center pt-3 pb-2 mb-3 border-bottom">
    <h1 class="h2">Gestão de Salas</h1>
    <?php if (pode('Administrador')): ?>
    <button class="btn btn-sm btn-primary" data-bs-toggle="modal" data-bs-target="#modalNovaSala"><i class="fas fa-plus me-1"></i> Nova Sala</button>
    <?php endif; ?>
</div>
<div class="card"><div class="card-body">
    <table class="table table-hover align-middle">
        <thead><tr><th>Número</th><th>Tipo</th><th>Status</th><th>Ações</th></tr></thead>
        <tbody>
        <?php foreach ($r->data ?? [] as $s): ?>
            <tr>
                <td><strong><?= e($s['numero']) ?></strong></td>
                <td><?= e($s['tipo']) ?></td>
                <td><span class="badge bg-<?= $s['status'] === 'Disponível' ? 'success' : ($s['status'] === 'Manutenção' ? 'secondary' : 'warning text-dark') ?>"><?= e($s['status']) ?></span></td>
                <td>
                    <?php if (pode('Administrador')): ?>
                    <form method="POST" action="processar_administrativo.php" class="d-inline-flex gap-1">
                        <input type="hidden" name="acao" value="editar_sala"><input type="hidden" name="id" value="<?= (int) $s['id'] ?>">
                        <input type="hidden" name="numero" value="<?= e($s['numero']) ?>"><input type="hidden" name="tipo" value="<?= e($s['tipo']) ?>">
                        <select name="status" class="form-select form-select-sm">
                            <?php foreach (['Disponível', 'Ocupada', 'Manutenção'] as $st): ?><option <?= $st === $s['status'] ? 'selected' : '' ?>><?= $st ?></option><?php endforeach; ?>
                        </select>
                        <button class="btn btn-sm btn-outline-secondary" title="Salvar status"><i class="fas fa-save"></i></button>
                    </form>
                    <form method="POST" action="processar_administrativo.php" class="d-inline" onsubmit="return confirm('Excluir esta sala?')">
                        <input type="hidden" name="acao" value="excluir_sala"><input type="hidden" name="id" value="<?= (int) $s['id'] ?>">
                        <button class="btn btn-sm btn-outline-danger"><i class="fas fa-trash"></i></button>
                    </form>
                    <?php endif; ?>
                </td>
            </tr>
        <?php endforeach; ?>
        <?php if (empty($r->data)): ?><tr><td colspan="4" class="text-center">Nenhuma sala cadastrada.</td></tr><?php endif; ?>
        </tbody>
    </table>
</div></div>

<div class="modal fade" id="modalNovaSala" tabindex="-1">
    <div class="modal-dialog"><div class="modal-content">
        <form action="processar_administrativo.php" method="POST">
            <input type="hidden" name="acao" value="nova_sala">
            <div class="modal-header"><h5 class="modal-title">Cadastrar Sala</h5><button type="button" class="btn-close" data-bs-dismiss="modal"></button></div>
            <div class="modal-body">
                <div class="mb-3"><label class="form-label">Número da Sala</label><input type="text" class="form-control" name="numero" maxlength="10" required></div>
                <div class="mb-3"><label class="form-label">Tipo</label>
                    <select class="form-select" name="tipo"><?php foreach ($TIPOS_SALA as $t): ?><option><?= e($t) ?></option><?php endforeach; ?></select>
                </div>
                <div class="mb-3 form-check">
                    <input type="checkbox" class="form-check-input" name="vincular_leito" id="vincular_leito" value="1">
                    <label class="form-check-label" for="vincular_leito">Vincular como Leito disponível</label>
                </div>
                <div class="mb-3"><label class="form-label">Ala (obrigatório se for leito)</label><input type="text" class="form-control" name="ala" placeholder="Ex: Ala Norte, UTI"></div>
            </div>
            <div class="modal-footer"><button type="submit" class="btn btn-primary">Salvar</button></div>
        </form>
    </div></div>
</div>
<?php require 'views/footer.php'; ?>
