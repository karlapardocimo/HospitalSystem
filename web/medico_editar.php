<?php
require_once 'lib/bootstrap.php';
exigir_login();
$id = (int) ($_GET['id'] ?? 0);
$r = api()->get("medicos/$id");
if (!$r->success) { flash_api($r); redirecionar('medicos.php'); }
$m = $r->data;
require 'views/header.php';
?>
<div class="d-flex justify-content-between align-items-center pt-3 pb-2 mb-3 border-bottom">
    <h1 class="h2">Editar Médico: <?= e($m['nome']) ?></h1>
    <a href="medicos.php" class="btn btn-sm btn-outline-secondary">Voltar</a>
</div>
<div class="row justify-content-center"><div class="col-lg-8"><div class="card"><div class="card-body">
    <form action="processar_medico.php" method="POST">
        <input type="hidden" name="acao" value="editar"><input type="hidden" name="id_medico" value="<?= (int) $m['id'] ?>">
        <?php require 'views/form_medico.php'; ?>
        <button type="submit" class="btn btn-primary w-100 mt-4">Salvar Alterações</button>
    </form>
</div></div></div></div>
<?php limpar_formulario(); require 'views/footer.php'; ?>
