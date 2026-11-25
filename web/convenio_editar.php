<?php
require_once 'lib/bootstrap.php';
exigir_login();
$id = (int) ($_GET['id'] ?? 0);
$r = api()->get("convenios/$id");
if (!$r->success) { flash_api($r); redirecionar('convenios.php'); }
$c = $r->data;
require 'views/header.php';
?>
<div class="d-flex justify-content-between align-items-center pt-3 pb-2 mb-3 border-bottom">
    <h1 class="h2">Editar Convênio</h1>
    <a href="convenios.php" class="btn btn-outline-secondary">Voltar</a>
</div>
<div class="card shadow-sm border-0"><div class="card-body">
    <form action="processar_administrativo.php" method="POST">
        <input type="hidden" name="acao" value="editar_convenio"><input type="hidden" name="id_convenio" value="<?= (int) $c['id'] ?>">
        <div class="row g-3">
            <div class="col-md-6"><label class="form-label">Nome do Convênio</label><input type="text" class="form-control" name="nome" value="<?= e($c['nome']) ?>" required></div>
            <div class="col-md-3"><label class="form-label">Validade</label><input type="date" class="form-control" name="validade" value="<?= e($c['validadeCarteira']) ?>"></div>
            <div class="col-md-3"><label class="form-label">Telefone</label><input type="text" class="form-control" name="telefone" value="<?= e($c['telefone']) ?>"></div>
            <div class="col-12 mt-4"><button type="submit" class="btn btn-primary px-4">Salvar Alterações</button> <a href="convenios.php" class="btn btn-light px-4">Cancelar</a></div>
        </div>
    </form>
</div></div>
<?php require 'views/footer.php'; ?>
