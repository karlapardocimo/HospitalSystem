<?php
// Convênios e salas: antes acessavam o MySQL, agora chamam a API.
require_once 'lib/bootstrap.php';
exigir_login();
if ($_SERVER['REQUEST_METHOD'] !== 'POST') redirecionar('index.php');

$acao = $_POST['acao'] ?? '';
$convenio = fn() => [
    'nome' => trim($_POST['nome'] ?? ''),
    'validadeCarteira' => nulo_se_vazio($_POST['validade'] ?? ''),
    'telefone' => nulo_se_vazio($_POST['telefone'] ?? ''),
];

[$r, $destino] = match ($acao) {
    'novo_convenio' => [api()->post('convenios', $convenio()), 'convenios.php'],
    'editar_convenio' => [api()->put('convenios/' . (int) $_POST['id_convenio'], $convenio()), 'convenios.php'],
    'excluir_convenio' => [api()->delete('convenios/' . (int) $_POST['id']), 'convenios.php'],
    'nova_sala' => [api()->post('salas', [
        'numero' => trim($_POST['numero'] ?? ''),
        'tipo' => $_POST['tipo'] ?? '',
        'vincularLeito' => !empty($_POST['vincular_leito']),
        'ala' => nulo_se_vazio($_POST['ala'] ?? ''),
    ]), 'salas.php'],
    'editar_sala' => [api()->put('salas/' . (int) $_POST['id'], [
        'numero' => $_POST['numero'] ?? '', 'tipo' => $_POST['tipo'] ?? '', 'status' => $_POST['status'] ?? '',
    ]), 'salas.php'],
    'excluir_sala' => [api()->delete('salas/' . (int) $_POST['id']), 'salas.php'],
    default => [null, 'index.php'],
};

if ($r) flash_api($r);
redirecionar($destino);
