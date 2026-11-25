<?php
require_once 'lib/bootstrap.php';
exigir_login();
if ($_SERVER['REQUEST_METHOD'] !== 'POST') redirecionar('faturamento.php');
$id = (int) ($_POST['id'] ?? 0);
$valor = nulo_se_vazio($_POST['valor'] ?? '');

$r = match ($_POST['acao'] ?? '') {
    'nova_guia' => api()->post('faturamentos', [
        'idPaciente' => (int) $_POST['id_paciente'],
        'valor' => $valor === null ? null : (float) str_replace(',', '.', $valor),
    ]),
    'pagar' => api()->patch("faturamentos/$id/pagar"),
    'auditoria' => api()->patch("faturamentos/$id/auditoria"),
    'aprovar' => api()->patch("faturamentos/$id/aprovar"),
    'cancelar' => api()->patch("faturamentos/$id/cancelar", ['motivo' => $_POST['motivo'] ?? '']),
    default => null,
};
if ($r) flash_api($r);
redirecionar('faturamento.php');
