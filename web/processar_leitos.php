<?php
require_once 'lib/bootstrap.php';
exigir_login();
if ($_SERVER['REQUEST_METHOD'] !== 'POST') redirecionar('leitos.php');
$id = (int) ($_POST['id'] ?? 0);

$r = match ($_POST['acao'] ?? '') {
    'novo' => api()->post('leitos', ['numero' => $_POST['numero'] ?? '', 'ala' => $_POST['ala'] ?? '']),
    'editar' => api()->put("leitos/$id", ['numero' => $_POST['numero'] ?? '', 'ala' => $_POST['ala'] ?? '']),
    'status' => api()->patch("leitos/$id/status", ['status' => $_POST['status'] ?? '']),   // RN-LEI-03/04 validadas na API
    'excluir' => api()->delete("leitos/$id"),
    default => null,
};
if ($r) flash_api($r);
redirecionar('leitos.php?filtro=' . urlencode($_POST['filtro'] ?? 'Todos'));
