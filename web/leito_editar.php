<?php
require_once 'lib/bootstrap.php';
exigir_login();
$id = (int) ($_GET['id'] ?? 0);
$r = api()->get("leitos/$id");
if (!$r->success) { flash_api($r); redirecionar('leitos.php'); }
$l = $r->data;
require 'views/header.php';
?>
<div class="d-flex justify-content-between align-items-center pt-3 pb-2 mb-3 border-bottom">
    <h1 class="h2">Editar Leito</h1>
    <a href="leitos.php" class="btn btn-sm btn-outline-secondary">Voltar</a>
</div>
<div class="row justify-content-center"><div class="col-lg-6"><div class="card"><div class="card-body">
    <form action="processar_leitos.php" method="POST">
        <input type="hidden" name="acao" value="editar"><input type="hidden" name="id" value="<?= (int) $l['id'] ?>">
        <div class="mb-3"><label class="form-label">Número do Leito</label><input type="text" class="form-control" name="numero" value="<?= e($l['numero']) ?>" required></div>
        <div class="mb-3"><label class="form-label">Ala / Setor</label><input type="text" class="form-control" name="ala" value="<?= e($l['ala']) ?>" required></div>
        <p class="small text-muted">Status atual: <strong><?= e($l['status']) ?></strong>. O status é alterado pelas ações do mapa de leitos ou pela internação.</p>
        <button type="submit" class="btn btn-primary w-100">Salvar Alterações</button>
    </form>
    <?php if ($l['status'] !== 'Ocupado'): ?>
    <form action="processar_leitos.php" method="POST" class="mt-2" onsubmit="return confirm('Excluir este leito?')">
        <input type="hidden" name="acao" value="excluir"><input type="hidden" name="id" value="<?= (int) $l['id'] ?>">
        <button class="btn btn-outline-danger w-100">Excluir leito</button>
    </form>
    <?php endif; ?>
</div></div></div></div>
<?php require 'views/footer.php'; ?>
