<?php
require_once 'lib/bootstrap.php';
exigir_login();
if ($_SERVER['REQUEST_METHOD'] !== 'POST') redirecionar('internacoes.php');
$id = (int) ($_POST['id'] ?? 0);

$r = match ($_POST['acao'] ?? '') {
    'internar' => api()->post('internacoes', ['idPaciente' => (int) $_POST['id_paciente'], 'idLeito' => (int) $_POST['id_leito']]),
    'alta' => api()->patch("internacoes/$id/alta"),
    'transferir' => api()->patch("internacoes/$id/transferencia", ['idNovoLeito' => (int) $_POST['id_novo_leito']]),
    default => null,
};
if ($r) flash_api($r);
redirecionar('internacoes.php');
