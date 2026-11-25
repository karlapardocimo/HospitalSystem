<?php
require_once 'lib/bootstrap.php';
exigir_login();
$r = api()->get('medicamentos');   // GET /api/medicamentos (com cache na API)
if (!$r->success) flash_api($r);
require 'views/header.php';
?>
<div class="d-flex justify-content-between align-items-center pt-3 pb-2 mb-3 border-bottom">
    <h1 class="h2">Farmácia e Medicamentos</h1>
    <?php if (pode('Administrador')): ?>
    <button class="btn btn-sm btn-primary" data-bs-toggle="modal" data-bs-target="#modalNovoMed"><i class="fas fa-pills me-1"></i> Novo Medicamento</button>
    <?php endif; ?>
</div>
<div class="card"><div class="card-body">
    <table class="table table-hover">
        <thead><tr><th>ID</th><th>Nome do Medicamento</th><th>Contraindicações</th><th>Interações</th><th>Ações</th></tr></thead>
        <tbody>
        <?php foreach ($r->data ?? [] as $m): ?>
            <tr>
                <td><?= (int) $m['id'] ?></td>
                <td><strong><?= e($m['nome']) ?></strong></td>
                <td class="small"><?= e($m['contraindicacoes'] ?? '-') ?></td>
                <td class="small"><?= e($m['interacoesMedicamentosas'] ?? '-') ?></td>
                <td>
                    <?php if (pode('Administrador')): ?>
                    <div class="btn-group">
                        <a href="medicamento_editar.php?id=<?= (int) $m['id'] ?>" class="btn btn-sm btn-outline-secondary"><i class="fas fa-edit"></i></a>
                        <form method="POST" action="processar_clinico.php" onsubmit="return confirm('Excluir este medicamento?')">
                            <input type="hidden" name="acao" value="excluir_medicamento"><input type="hidden" name="id" value="<?= (int) $m['id'] ?>">
                            <button class="btn btn-sm btn-outline-danger"><i class="fas fa-trash"></i></button>
                        </form>
                    </div>
                    <?php endif; ?>
                </td>
            </tr>
        <?php endforeach; ?>
        <?php if (empty($r->data)): ?><tr><td colspan="5" class="text-center">Nenhum medicamento cadastrado.</td></tr><?php endif; ?>
        </tbody>
    </table>
</div></div>

<div class="modal fade" id="modalNovoMed" tabindex="-1">
    <div class="modal-dialog"><div class="modal-content">
        <form action="processar_clinico.php" method="POST">
            <input type="hidden" name="acao" value="novo_medicamento">
            <div class="modal-header"><h5 class="modal-title">Novo Medicamento</h5><button type="button" class="btn-close" data-bs-dismiss="modal"></button></div>
            <div class="modal-body">
                <div class="mb-3"><label class="form-label">Nome</label><input type="text" class="form-control" name="nome" required></div>
                <div class="mb-3"><label class="form-label">Contraindicações</label><textarea class="form-control" name="contra" rows="2"></textarea></div>
                <div class="mb-3"><label class="form-label">Interações (nomes dos medicamentos, separados por vírgula)</label><textarea class="form-control" name="inter" rows="2"></textarea></div>
            </div>
            <div class="modal-footer"><button type="submit" class="btn btn-primary">Salvar</button></div>
        </form>
    </div></div>
</div>
<?php require 'views/footer.php'; ?>
