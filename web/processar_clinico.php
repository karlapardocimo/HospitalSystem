<?php
require_once 'lib/bootstrap.php';
exigir_login();
if ($_SERVER['REQUEST_METHOD'] !== 'POST') redirecionar('medicamentos.php');

$dados = fn() => [
    'nome' => trim($_POST['nome'] ?? ''),
    'contraindicacoes' => nulo_se_vazio($_POST['contra'] ?? ''),
    'interacoesMedicamentosas' => nulo_se_vazio($_POST['inter'] ?? ''),
];

$r = match ($_POST['acao'] ?? '') {
    'novo_medicamento' => api()->post('medicamentos', $dados()),
    'editar_medicamento' => api()->put('medicamentos/' . (int) $_POST['id_medicamento'], $dados()),
    'excluir_medicamento' => api()->delete('medicamentos/' . (int) $_POST['id']),
    default => null,
};
if ($r) flash_api($r);
redirecionar('medicamentos.php');
