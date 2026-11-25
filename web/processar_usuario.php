<?php
require_once 'lib/bootstrap.php';
exigir_login();
if ($_SERVER['REQUEST_METHOD'] !== 'POST') redirecionar('usuarios.php');
$id = (int) ($_POST['id'] ?? 0);

$r = match ($_POST['acao'] ?? '') {
    'novo' => api()->post('usuarios', [
        'nome' => trim($_POST['nome'] ?? ''), 'email' => trim($_POST['email'] ?? ''),
        'senha' => $_POST['senha'] ?? '', 'perfil' => $_POST['perfil'] ?? '',
    ]),
    'editar' => api()->put("usuarios/$id", [
        'nome' => $_POST['nome'] ?? '', 'perfil' => $_POST['perfil'] ?? '', 'ativo' => ($_POST['ativo'] ?? '1') === '1',
    ]),
    'senha' => api()->patch("usuarios/$id/senha", ['novaSenha' => $_POST['nova_senha'] ?? '']),
    'excluir' => api()->delete("usuarios/$id"),
    default => null,
};
if ($r) flash_api($r);
redirecionar('usuarios.php');
