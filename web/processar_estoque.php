<?php
require_once 'lib/bootstrap.php';
exigir_login();
if ($_SERVER['REQUEST_METHOD'] !== 'POST') redirecionar('estoque.php');
$id = (int) ($_POST['id_insumo'] ?? 0);

$r = match ($_POST['acao'] ?? '') {
    'novo' => api()->post('insumos', [
        'nome' => trim($_POST['nome'] ?? ''),
        'quantidadeInicial' => (int) $_POST['quantidade'],
        'valorUnitario' => (float) str_replace(',', '.', $_POST['valor']),
    ]),
    'ajuste' => api()->post("insumos/$id/movimentacoes", ['tipo' => $_POST['operacao'], 'quantidade' => (int) $_POST['quantidade']]),
    'uso' => api()->post("insumos/$id/usos", ['idLeito' => (int) $_POST['id_leito'], 'quantidade' => (int) $_POST['quantidade']]),
    default => null,
};
if ($r) flash_api($r);
redirecionar('estoque.php');
