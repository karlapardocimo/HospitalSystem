<?php
// Incluído no topo de todas as páginas: sessão, configuração, cliente da API e autenticação.
if (session_status() !== PHP_SESSION_ACTIVE) {
    session_start();
}
require_once __DIR__ . '/../config/config.php';
require_once __DIR__ . '/../config/helpers.php';
require_once __DIR__ . '/ApiClient.php';

/** Cliente autenticado com o token JWT guardado na sessão. */
function api(): ApiClient
{
    static $cliente = null;
    if ($cliente === null) {
        $cliente = new ApiClient(API_BASE_URL, $_SESSION['token'] ?? null, function (ApiResponse $r) {
            encerrar_sessao();
            session_start();
            flash('warning', $r->message ?: 'Sua sessão expirou. Faça login novamente.');
            header('Location: login.php');
            exit;
        });
    }
    return $cliente;
}

function usuario_logado(): ?array
{
    return $_SESSION['usuario'] ?? null;
}

/** O perfil do usuário logado está entre os informados? (apenas para esconder botões; quem decide é a API) */
function pode(string ...$perfis): bool
{
    $u = usuario_logado();
    return $u !== null && in_array($u['perfil'], $perfis, true);
}

/** Redireciona para o login se não houver token válido na sessão. */
function exigir_login(): void
{
    $expira = $_SESSION['token_expira'] ?? 0;
    if (empty($_SESSION['token']) || time() >= $expira) {
        if (!empty($_SESSION['token'])) {
            encerrar_sessao();
            session_start();
            flash('warning', 'Sua sessão expirou. Faça login novamente.');
        }
        header('Location: login.php');
        exit;
    }
}

function encerrar_sessao(): void
{
    $_SESSION = [];
    if (session_status() === PHP_SESSION_ACTIVE) {
        session_destroy();
    }
}
