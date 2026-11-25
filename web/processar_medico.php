<?php
require_once 'lib/bootstrap.php';
exigir_login();
if ($_SERVER['REQUEST_METHOD'] !== 'POST') redirecionar('medicos.php');
$acao = $_POST['acao'] ?? '';

if ($acao === 'excluir') {
    flash_api(api()->delete('medicos/' . (int) $_POST['id']));   // DELETE /api/medicos/{id}
    redirecionar('medicos.php');
}

$dados = [
    'nome' => trim($_POST['nome'] ?? ''),
    'crm' => trim($_POST['crm'] ?? ''),
    'especialidade' => $_POST['especialidade'] ?? '',
    'telefone' => nulo_se_vazio($_POST['telefone'] ?? ''),
    'email' => nulo_se_vazio($_POST['email'] ?? ''),
];

if ($acao === 'editar') {
    $id = (int) $_POST['id_medico'];
    $r = api()->put("medicos/$id", $dados);                         // PUT /api/medicos/{id}
    $erro = "medico_editar.php?id=$id";
} else {
    $r = api()->post('medicos', $dados);                            // POST /api/medicos
    $erro = 'medico_novo.php';
}
flash_api($r);
if ($r->success) redirecionar('medicos.php');
guardar_formulario($_POST);
redirecionar($erro);
