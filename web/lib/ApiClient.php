<?php
/**
 * Resposta padronizada da API (ResultWrapper):
 * { "success": bool, "message": string, "data": mixed, "errors": string[] }
 */
class ApiResponse
{
    public function __construct(
        public bool $success,
        public string $message,
        public mixed $data,
        public array $errors,
        public int $status
    ) {}

    /** Mensagem completa para exibir ao usuário (mensagem + lista de erros). */
    public function errosTexto(): string
    {
        return implode(' ', $this->errors);
    }
}

/**
 * Cliente HTTP da API (cURL).
 * - envia e recebe JSON;
 * - anexa o token JWT no cabeçalho Authorization: Bearer <token>;
 * - converte qualquer resposta (inclusive erros de rede) em ApiResponse;
 * - quando a API responde 401 numa rota protegida, chama $aoExpirar (volta para o login).
 */
class ApiClient
{
    private $aoExpirar;

    public function __construct(
        private string $baseUrl,
        private ?string $token = null,
        ?callable $aoExpirar = null
    ) {
        $this->aoExpirar = $aoExpirar;
    }

    public function get(string $rota, array $query = []): ApiResponse
    {
        return $this->requisitar('GET', $rota, null, $query);
    }

    public function post(string $rota, array $corpo = []): ApiResponse
    {
        return $this->requisitar('POST', $rota, $corpo);
    }

    public function put(string $rota, array $corpo = []): ApiResponse
    {
        return $this->requisitar('PUT', $rota, $corpo);
    }

    public function patch(string $rota, array $corpo = []): ApiResponse
    {
        return $this->requisitar('PATCH', $rota, $corpo);
    }

    public function delete(string $rota): ApiResponse
    {
        return $this->requisitar('DELETE', $rota);
    }

    private function requisitar(string $metodo, string $rota, ?array $corpo = null, array $query = []): ApiResponse
    {
        $query = array_filter($query, fn($v) => $v !== null && $v !== '');
        $url = $this->baseUrl . '/' . ltrim($rota, '/') . ($query ? '?' . http_build_query($query) : '');

        $cabecalhos = ['Accept: application/json'];
        if ($this->token) {
            $cabecalhos[] = 'Authorization: Bearer ' . $this->token;
        }

        $ch = curl_init($url);
        curl_setopt_array($ch, [
            CURLOPT_CUSTOMREQUEST => $metodo,
            CURLOPT_RETURNTRANSFER => true,
            CURLOPT_TIMEOUT => API_TIMEOUT,
            CURLOPT_CONNECTTIMEOUT => 5,
        ]);
        if ($corpo !== null) {
            $cabecalhos[] = 'Content-Type: application/json';
            curl_setopt($ch, CURLOPT_POSTFIELDS, json_encode($corpo, JSON_UNESCAPED_UNICODE));
        }
        curl_setopt($ch, CURLOPT_HTTPHEADER, $cabecalhos);

        $bruto = curl_exec($ch);
        $status = (int) curl_getinfo($ch, CURLINFO_HTTP_CODE);
        $erroRede = curl_error($ch);
        curl_close($ch);

        if ($bruto === false) {
            return new ApiResponse(false, 'Não foi possível conectar à API.', null,
                ["Verifique se a API está em execução em " . API_BASE_URL . " ($erroRede)."], 0);
        }

        $json = json_decode($bruto, true);
        if (!is_array($json) || !array_key_exists('success', $json)) {
            return new ApiResponse(false, 'Resposta inesperada da API.', null, ["HTTP $status"], $status);
        }

        $resposta = new ApiResponse(
            (bool) $json['success'],
            (string) ($json['message'] ?? ''),
            $json['data'] ?? null,
            $json['errors'] ?? [],
            $status
        );

        // Token expirado ou inválido: encerra a sessão e volta para o login.
        if ($status === 401 && $this->token && $this->aoExpirar) {
            ($this->aoExpirar)($resposta);
        }
        return $resposta;
    }
}
