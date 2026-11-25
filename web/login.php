<?php
require_once 'lib/bootstrap.php';

// Já autenticado? vai para o dashboard.
if (!empty($_SESSION['token']) && time() < ($_SESSION['token_expira'] ?? 0)) {
    redirecionar('index.php');
}

$email = '';
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $email = trim($_POST['email'] ?? '');
    // Cliente SEM token: o login é um endpoint público.
    $resposta = (new ApiClient(API_BASE_URL))->post('auth/login', [
        'email' => $email,
        'senha' => $_POST['senha'] ?? '',
    ]);

    if ($resposta->success) {
        session_regenerate_id(true);
        $_SESSION['token'] = $resposta->data['token'];                       // JWT guardado na sessão do servidor
        $_SESSION['token_expira'] = strtotime($resposta->data['expiraEm']);
        $_SESSION['usuario'] = $resposta->data['usuario'];
        flash('success', 'Bem-vindo(a), ' . $resposta->data['usuario']['nome'] . '!');
        redirecionar('index.php');
    }
    flash_api($resposta);
}
?>
<!DOCTYPE html>
<html lang="pt-br">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Login - <?= e(APP_NAME) ?></title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet">
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css">
    <style> body { background: linear-gradient(135deg, #2c3e50, #3498db); min-height: 100vh; } </style>
</head>
<body class="d-flex align-items-center">
<div class="container">
    <div class="row justify-content-center">
        <div class="col-md-5 col-lg-4">
            <div class="card shadow border-0">
                <div class="card-body p-4">
                    <div class="text-center mb-4">
                        <i class="fas fa-hospital-user fa-3x text-primary mb-2"></i>
                        <h1 class="h4 mb-0">Manus Hospital</h1>
                        <small class="text-muted"><?= e(APP_NAME) ?></small>
                    </div>
                    <?php render_flash(); ?>
                    <form method="POST" autocomplete="on">
                        <div class="mb-3">
                            <label class="form-label" for="email">E-mail</label>
                            <input type="email" class="form-control" id="email" name="email" value="<?= e($email) ?>" required autofocus>
                        </div>
                        <div class="mb-3">
                            <label class="form-label" for="senha">Senha</label>
                            <input type="password" class="form-control" id="senha" name="senha" required>
                        </div>
                        <button type="submit" class="btn btn-primary w-100"><i class="fas fa-sign-in-alt me-1"></i> Entrar</button>
                    </form>
                </div>
                <div class="card-footer bg-white small text-muted text-center">
                    Autenticação via API (JWT) — <?= e(API_BASE_URL) ?>
                </div>
            </div>
        </div>
    </div>
</div>
<script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js"></script>
</body>
</html>
