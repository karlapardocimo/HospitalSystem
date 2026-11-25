-- Migração de um banco criado pelo sistema PHP (versão 1) para a versão da API.
-- NÃO é obrigatório: a API aplica estas alterações sozinha ao iniciar
-- (Database:InicializarAutomaticamente = true). Use este script só se quiser migrar manualmente.
-- Sintaxe "ADD COLUMN IF NOT EXISTS" é do MariaDB (XAMPP). No MySQL 8, remova o "IF NOT EXISTS".
USE hospital;

CREATE TABLE IF NOT EXISTS usuarios (
    id_usuario INT PRIMARY KEY AUTO_INCREMENT,
    nome VARCHAR(100) NOT NULL,
    email VARCHAR(150) NOT NULL UNIQUE,
    senha_hash VARCHAR(255) NOT NULL,
    perfil VARCHAR(20) NOT NULL,
    ativo TINYINT(1) NOT NULL DEFAULT 1,
    tentativas_falhas INT NOT NULL DEFAULT 0,
    bloqueado_ate DATETIME NULL,
    criado_em DATETIME NOT NULL
);

ALTER TABLE triagem     ADD COLUMN IF NOT EXISTS classificacao_calculada VARCHAR(20) NULL;
ALTER TABLE triagem     ADD COLUMN IF NOT EXISTS queixa_principal TEXT NULL;
ALTER TABLE triagem     ADD COLUMN IF NOT EXISTS data_triagem DATETIME NULL;
ALTER TABLE triagem     ADD COLUMN IF NOT EXISTS id_consulta INT NULL;
ALTER TABLE prontuario  ADD COLUMN IF NOT EXISTS profissional VARCHAR(150) NULL;
ALTER TABLE prescricoes ADD COLUMN IF NOT EXISTS data_prescricao DATETIME NULL;
ALTER TABLE prescricoes ADD COLUMN IF NOT EXISTS alerta_confirmado TEXT NULL;
ALTER TABLE faturamento ADD COLUMN IF NOT EXISTS data_pagamento DATE NULL;
ALTER TABLE uso_insumos ADD COLUMN IF NOT EXISTS id_internacao INT NULL;

-- O primeiro administrador é criado pela API (Seed:AdminEmail / Seed:AdminSenha),
-- pois a senha precisa ser gravada com hash PBKDF2.
