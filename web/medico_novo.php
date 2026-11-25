<?php
require_once 'lib/bootstrap.php';
exigir_login();
$m = [];
require 'views/header.php';
?>
<div class="d-flex justify-content-between align-items-center pt-3 pb-2 mb-3 border-bottom">
    <h1 class="h2">Cadastrar Médico</h1>
    <a href="medicos.php" class="btn btn-sm btn-outline-secondary">Voltar</a>
</div>
<div class="row justify-content-center"><div class="col-lg-8"><div class="card"><div class="card-body">
    <form action="processar_medico.php" method="POST">
        <input type="hidden" name="acao" value="cadastrar">
        <?php require 'views/form_medico.php'; ?>
        <button type="submit" class="btn btn-primary w-100 mt-4">Salvar Médico</button>
    </form>
</div></div></div></div>
<?php limpar_formulario(); require 'views/footer.php'; ?>
