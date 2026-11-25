<?php
require_once 'lib/bootstrap.php';
exigir_login();
$id = (int) ($_GET['id'] ?? 0);
$r = api()->get("medicamentos/$id");
if (!$r->success) { flash_api($r); redirecionar('medicamentos.php'); }
$m = $r->data;
require 'views/header.php';
?>
<div class="d-flex justify-content-between align-items-center pt-3 pb-2 mb-3 border-bottom">
    <h1 class="h2">Editar Medicamento</h1>
    <a href="medicamentos.php" class="btn btn-sm btn-outline-secondary">Voltar</a>
</div>
<div class="row justify-content-center"><div class="col-lg-6"><div class="card"><div class="card-body">
    <form action="processar_clinico.php" method="POST">
        <input type="hidden" name="acao" value="editar_medicamento"><input type="hidden" name="id_medicamento" value="<?= (int) $m['id'] ?>">
        <div class="mb-3"><label class="form-label">Nome</label><input type="text" class="form-control" name="nome" value="<?= e($m['nome']) ?>" required></div>
        <div class="mb-3"><label class="form-label">Contraindicações</label><textarea class="form-control" name="contra" rows="3"><?= e($m['contraindicacoes']) ?></textarea></div>
        <div class="mb-3"><label class="form-label">Interações Medicamentosas</label><textarea class="form-control" name="inter" rows="3"><?= e($m['interacoesMedicamentosas']) ?></textarea></div>
        <button type="submit" class="btn btn-primary w-100">Salvar Alterações</button>
    </form>
</div></div></div></div>
<?php require 'views/footer.php'; ?>
