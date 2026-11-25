<?php
require_once 'lib/bootstrap.php';
exigir_login();

$id = (int) ($_GET['id'] ?? 0);
$r = api()->get("pacientes/$id");
if (!$r->success) {
    flash_api($r);
    redirecionar('pacientes.php');
}
$p = $r->data;
$edicao = true;
require 'views/header.php';
?>
<div class="d-flex justify-content-between flex-wrap flex-md-nowrap align-items-center pt-3 pb-2 mb-3 border-bottom">
    <h1 class="h2">Editar Paciente: <?= e($p['nome']) ?></h1>
    <a href="pacientes.php" class="btn btn-sm btn-outline-secondary"><i class="fas fa-arrow-left me-1"></i> Voltar</a>
</div>
<div class="row justify-content-center">
    <div class="col-lg-10">
        <div class="card">
            <div class="card-body">
                <form action="processar_paciente.php" method="POST">
                    <input type="hidden" name="acao" value="editar">
                    <input type="hidden" name="id_paciente" value="<?= (int) $p['id'] ?>">
                    <?php require 'views/form_paciente.php'; ?>
                    <hr class="mt-4">
                    <div class="d-flex justify-content-end gap-2">
                        <a href="pacientes.php" class="btn btn-light">Cancelar</a>
                        <button type="submit" class="btn btn-primary px-4">Salvar Alterações</button>
                    </div>
                </form>
            </div>
        </div>
    </div>
</div>
<?php limpar_formulario(); require 'views/footer.php'; ?>
