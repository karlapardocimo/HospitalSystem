<?php
require_once 'lib/bootstrap.php';
// O JWT não fica guardado na API (stateless): sair = descartar o token da sessão.
encerrar_sessao();
session_start();
flash('info', 'Você saiu do sistema.');
redirecionar('login.php');
