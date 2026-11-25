# Regras de Negócio — Sistema de Gestão Hospitalar

Este documento descreve as regras de negócio do sistema. Elas foram levantadas a partir do
sistema PHP desenvolvido na etapa anterior (telas, *models*, `config/config.php` e
`config/helpers.php`) e agora são aplicadas **pela API**, não mais pelo aplicativo.

Cada regra tem um código (`RN-XXX-NN`). O mesmo código aparece nas mensagens de erro da API,
o que permite rastrear qualquer resposta de erro até a regra que a gerou. Exemplo:

```json
{
  "success": false,
  "message": "A operação viola regras de negócio.",
  "data": null,
  "errors": ["RN-PAC-02: CPF inválido."]
}
```

Onde a regra está no código:

| Tipo de regra | Camada | Onde |
|---|---|---|
| Invariantes da entidade (campos obrigatórios, formatos, transições de status) | Domain | `HospitalSystem.Domain/Entities/*.cs` |
| Cálculos de domínio (Protocolo de Manchester, interações medicamentosas) | Domain | `HospitalSystem.Domain/Services/*.cs` |
| Regras que dependem de outros registros (unicidade, conflitos de horário, vínculos) | Application | `HospitalSystem.Application/Services/*.cs` |
| Permissões por perfil | API | atributos `[Authorize(Roles = ...)]` nos controllers |

---

## 1. Usuários, autenticação e perfis (RN-USU)

| Código | Regra |
|---|---|
| RN-USU-01 | Nome do usuário é obrigatório (3 a 100 caracteres). |
| RN-USU-02 | E-mail é obrigatório, deve ter formato válido e é único no sistema (sem diferenciar maiúsculas/minúsculas). |
| RN-USU-03 | A senha deve ter no mínimo 8 caracteres, com pelo menos uma letra e um número. |
| RN-USU-04 | A senha nunca é armazenada em texto puro. É guardado apenas o *hash* PBKDF2-SHA256 com 100.000 iterações e *salt* aleatório de 16 bytes por usuário. |
| RN-USU-05 | Todo usuário tem um único perfil: `Administrador`, `Medico`, `Enfermagem` ou `Recepcao`. |
| RN-USU-06 | Somente o `Administrador` cadastra, altera, redefine senha ou remove usuários. |
| RN-USU-07 | Em caso de falha no login, a mensagem é genérica ("E-mail ou senha inválidos"), sem revelar se o e-mail existe. |
| RN-USU-08 | Após 5 tentativas de login erradas seguidas, a conta fica bloqueada por 15 minutos. Um login correto zera o contador. |
| RN-USU-09 | Usuário inativo não consegue se autenticar. |
| RN-USU-10 | O token JWT expira em 60 minutos (configurável) e carrega id, nome, e-mail e perfil do usuário. |
| RN-USU-11 | O administrador não pode desativar nem remover a própria conta. |
| RN-USU-12 | Na primeira execução, se não houver nenhum usuário, o sistema cria o administrador inicial a partir da configuração (nunca com senha fixa no código). |

### Matriz de permissões

| Funcionalidade | Administrador | Medico | Enfermagem | Recepcao |
|---|:-:|:-:|:-:|:-:|
| Consultar qualquer cadastro / dashboard | ✔ | ✔ | ✔ | ✔ |
| Cadastrar e editar pacientes | ✔ | | ✔ | ✔ |
| Excluir pacientes | ✔ | | | |
| Médicos, convênios, salas, medicamentos (escrita) | ✔ | | | |
| Agendar, enviar à triagem, cancelar consultas | ✔ | | | ✔ |
| Registrar triagem | ✔ | ✔ | ✔ | |
| Liberar paciente / registrar evolução no prontuário | ✔ | ✔ | ✔ (só evolução) | |
| Prescrever medicamentos | | ✔ | | |
| Solicitar exames e registrar resultado | ✔ | ✔ | | |
| Leitos e internações | ✔ | ✔ | ✔ | |
| Dar alta | ✔ | ✔ | | |
| Estoque de insumos | ✔ | | ✔ | |
| Faturamento | ✔ | | | ✔ |
| Usuários e limpeza do cache | ✔ | | | |

> A prescrição é exclusiva do perfil `Medico`, nem o administrador prescreve (ato médico).

---

## 2. Pacientes (RN-PAC)

| Código | Regra |
|---|---|
| RN-PAC-01 | Nome obrigatório, entre 3 e 100 caracteres. |
| RN-PAC-02 | CPF obrigatório e válido (dígitos verificadores). É armazenado apenas com os 11 dígitos e formatado na exibição. |
| RN-PAC-03 | Não podem existir dois pacientes com o mesmo CPF. |
| RN-PAC-04 | Data de nascimento obrigatória, não pode ser futura e a idade máxima aceita é 130 anos. |
| RN-PAC-05 | Tipo sanguíneo, quando informado, deve ser um de: O+, O-, A+, A-, B+, B-, AB+, AB-. |
| RN-PAC-06 | Telefone, quando informado, deve ter 10 ou 11 dígitos (com DDD). |
| RN-PAC-07 | Ao cadastrar um paciente ele é enviado automaticamente para a fila de triagem (cria uma consulta "Aguardando Triagem"), a menos que o cadastro indique o contrário. *(comportamento herdado do sistema PHP)* |
| RN-PAC-08 | Um paciente só pode ser excluído se não tiver consultas, internações ou faturamentos. O histórico clínico não pode ser apagado. |
| RN-PAC-09 | As alergias registradas são verificadas em toda prescrição (ver RN-PRE-03). |

## 3. Médicos (RN-MED)

| Código | Regra |
|---|---|
| RN-MED-01 | Nome obrigatório, entre 3 e 100 caracteres. |
| RN-MED-02 | CRM obrigatório, com 4 a 10 dígitos numéricos. |
| RN-MED-03 | O CRM é único no sistema. |
| RN-MED-04 | Especialidade obrigatória, dentre: Cardiologia, Neurologia, Ortopedia, Pediatria, Ginecologia, Oftalmologia, Otorrinolaringologia, Dermatologia, Psiquiatria, Cirurgia Geral, Clínica Geral. |
| RN-MED-05 | E-mail, quando informado, deve ter formato válido. |
| RN-MED-06 | Médico com consultas vinculadas não pode ser excluído. |

## 4. Convênios (RN-CON)

| Código | Regra |
|---|---|
| RN-CON-01 | Nome do convênio obrigatório e único. |
| RN-CON-02 | Convênio com consultas vinculadas não pode ser excluído. |
| RN-CON-03 | Convênio com validade vencida não pode ser usado em consulta cuja data seja posterior à validade. |

## 5. Salas (RN-SAL)

| Código | Regra |
|---|---|
| RN-SAL-01 | Número da sala obrigatório e único. |
| RN-SAL-02 | Tipo obrigatório, dentre: Consultório, Cirurgia, Exame, Triagem, Internação. |
| RN-SAL-03 | Status possíveis: Disponível, Ocupada, Manutenção. Toda sala nova começa Disponível. |
| RN-SAL-04 | Ao cadastrar uma sala é possível gerar um leito vinculado com o mesmo número; nesse caso a ala é obrigatória. *(comportamento herdado do sistema PHP)* |
| RN-SAL-05 | Sala em Manutenção não pode receber consultas. |
| RN-SAL-06 | Sala com consultas vinculadas não pode ser excluída. |

## 6. Consultas e fila (RN-CNS)

| Código | Regra |
|---|---|
| RN-CNS-01 | Paciente obrigatório e existente. |
| RN-CNS-02 | Agendamento exige um médico existente. (A entrada automática na fila pelo cadastro do paciente dispensa médico.) |
| RN-CNS-03 | Data e horário da consulta não podem estar no passado (tolerância de 10 minutos). |
| RN-CNS-04 | Um médico não pode ter duas consultas ativas no mesmo dia e horário. |
| RN-CNS-05 | Um paciente não pode ter duas consultas ativas no mesmo dia e horário. |
| RN-CNS-06 | Consulta ativa é aquela com status Agendada, Aguardando Triagem ou Em Atendimento. |
| RN-CNS-07 | As mudanças de status permitidas são somente as do diagrama abaixo. Liberado, Cancelada e Não Compareceu são status finais. |
| RN-CNS-08 | Só pode ser cancelada consulta Agendada ou Aguardando Triagem. |
| RN-CNS-09 | Só pode ser marcada "Não Compareceu" consulta Agendada. |
| RN-CNS-10 | Só pode ser liberado o paciente cuja consulta está Em Atendimento. |

```
            enviar p/ triagem             triagem                 liberar
 Agendada ───────────────────► Aguardando ─────────► Em Atendimento ───────► Liberado
    │  │                        Triagem
    │  └─ cancelar ──► Cancelada ◄── cancelar ──┘
    └──── não compareceu ──► Não Compareceu
```

## 7. Triagem — Protocolo de Manchester (RN-TRI)

| Código | Regra |
|---|---|
| RN-TRI-01 | Só é triada consulta com status Aguardando Triagem. Depois da triagem a consulta passa para Em Atendimento. |
| RN-TRI-02 | Faixas aceitas: temperatura 30,0–45,0 °C; frequência cardíaca 20–250 bpm; saturação 50–100 %; dor 0–10. |
| RN-TRI-03 | Pressão arterial no formato `sistólica/diastólica` em mmHg (ex.: `120/80`). O formato em cmHg usado no sistema antigo (`12/8`) é aceito e convertido. A sistólica deve ser maior que a diastólica. |
| RN-TRI-04 | A classificação de risco é **calculada automaticamente** pelos sinais vitais (tabela abaixo). |
| RN-TRI-05 | O profissional pode **elevar** a classificação calculada (julgamento clínico), mas nunca reduzi-la. |
| RN-TRI-06 | A fila de atendimento é ordenada por gravidade (Vermelho → Azul) e, dentro da mesma cor, por ordem de chegada (quem foi triado primeiro é atendido primeiro). |
| RN-TRI-07 | Cada cor tem um tempo-alvo de atendimento. Pacientes que passaram do tempo são sinalizados como "atrasados" na fila. |

| Cor | Prioridade | Critério (basta um) | Tempo-alvo |
|---|---|---|---|
| Vermelho | Emergência | Temp. > 40 °C, Sat. < 90 %, FC > 130, dor ≥ 8 | imediato (0 min) |
| Laranja | Muito urgente | Temp. > 39 °C, Sat. < 93 %, FC > 110, dor ≥ 7 | 10 min |
| Amarelo | Urgente | Temp. > 38 °C, Sat. < 95 %, FC > 100, dor ≥ 5 | 60 min |
| Verde | Pouco urgente | dor ≥ 2 | 120 min |
| Azul | Não urgente | nenhum dos anteriores | 240 min |

*(Critérios idênticos à função `classificarRiscoManchester()` do sistema PHP, que existia mas não era utilizada.)*

## 8. Prontuário eletrônico (RN-PRT)

| Código | Regra |
|---|---|
| RN-PRT-01 | Registro de evolução clínica só pode ser feito em consulta Em Atendimento. |
| RN-PRT-02 | A evolução clínica é obrigatória, com no mínimo 10 caracteres. |
| RN-PRT-03 | Todo registro guarda automaticamente o autor (usuário do token) e a data/hora. |
| RN-PRT-04 | Registros de prontuário não podem ser alterados nem excluídos (documento legal); correções são feitas por um novo registro. |

## 9. Prescrições (RN-PRE)

| Código | Regra |
|---|---|
| RN-PRE-01 | Somente usuários com perfil `Medico` podem prescrever. |
| RN-PRE-02 | Medicamento existente; dosagem, frequência e duração do tratamento são obrigatórias. |
| RN-PRE-03 | A prescrição é **bloqueada** se as alergias do paciente mencionarem o medicamento. |
| RN-PRE-04 | Se o medicamento tiver interação cadastrada com outro medicamento prescrito ao mesmo paciente nos últimos 30 dias, a prescrição é bloqueada e o alerta é devolvido. O médico pode confirmar conscientemente (`confirmarInteracao = true`) e a prescrição é gravada com o alerta. |
| RN-PRE-05 | As contraindicações do medicamento são devolvidas como alerta junto com a prescrição. |

## 10. Exames (RN-EXA)

| Código | Regra |
|---|---|
| RN-EXA-01 | Nome do exame obrigatório. A data é obrigatória (padrão: hoje). |
| RN-EXA-02 | O resultado pode ser registrado depois da solicitação, uma única vez. |

## 11. Leitos (RN-LEI)

| Código | Regra |
|---|---|
| RN-LEI-01 | Número e ala obrigatórios. Não pode haver dois leitos com o mesmo número na mesma ala. |
| RN-LEI-02 | Status possíveis: Disponível, Ocupado, Higienização, Manutenção. Todo leito novo começa Disponível. |
| RN-LEI-03 | Um leito só fica Ocupado por meio de uma internação, e só deixa de estar Ocupado por alta ou transferência. Não é possível "ocupar" ou "liberar" um leito ocupado manualmente. |
| RN-LEI-04 | Mudanças manuais permitidas: Disponível ↔ Manutenção, Disponível → Higienização, Higienização → Disponível, Higienização → Manutenção. |
| RN-LEI-05 | Só pode ser excluído leito que não esteja Ocupado e que não tenha histórico de internações. |

## 12. Internações (RN-INT)

| Código | Regra |
|---|---|
| RN-INT-01 | Paciente existente e sem outra internação ativa. |
| RN-INT-02 | O leito escolhido precisa estar Disponível. |
| RN-INT-03 | Ao internar, o leito passa para Ocupado na mesma transação. |
| RN-INT-04 | Alta só para internação Ativa: registra data/hora da alta e o leito vai para Higienização. |
| RN-INT-05 | Transferência: o novo leito precisa estar Disponível; o antigo vai para Higienização e o novo para Ocupado, na mesma transação. |
| RN-INT-06 | "Pacientes internados" (dashboard) são as internações com status Ativa. |

## 13. Estoque de insumos (RN-EST)

| Código | Regra |
|---|---|
| RN-EST-01 | Nome obrigatório e único; quantidade inicial ≥ 0; valor unitário > 0. |
| RN-EST-02 | Toda movimentação tem quantidade maior que zero. Uma saída não pode deixar o estoque negativo. |
| RN-EST-03 | Insumo com menos de 20 unidades é sinalizado como estoque crítico. |
| RN-EST-04 | O uso de insumo em paciente é lançado em um leito Ocupado (com internação ativa) e dá baixa automática no estoque. |
| RN-EST-05 | A quantidade em estoque não é editada diretamente, só por movimentação (entrada, saída ou uso), para manter a rastreabilidade. |

## 14. Faturamento (RN-FAT)

| Código | Regra |
|---|---|
| RN-FAT-01 | Paciente existente e valor total maior que zero. |
| RN-FAT-02 | Consolidação de gastos do paciente = insumos usados nos leitos durante as internações dele (quantidade × valor unitário) + exames × R$ 100,00 + prescrições × R$ 50,00. *(valores de referência herdados de `gerarRelatorioGastos()`)* |
| RN-FAT-03 | Guia emitida sem valor informado usa o valor consolidado. Se o consolidado for zero, a guia não é emitida. |
| RN-FAT-04 | Toda guia nasce Pendente. Guias de R$ 10.000,00 ou mais vão automaticamente para Auditoria e geram um registro na tabela `auditoria`. |
| RN-FAT-05 | Transições: Pendente → Pago / Auditoria / Cancelado; Auditoria → Pendente (aprovada) / Cancelado. Pago e Cancelado são finais. |
| RN-FAT-06 | O cancelamento exige motivo, que fica registrado na auditoria. |
| RN-FAT-07 | Indicadores: total pendente, total recebido **no mês corrente** e quantidade de guias em auditoria. |

---

## Correções em relação ao sistema anterior

Algumas regras acima corrigem comportamentos do sistema PHP que estavam inconsistentes:

- `validarCPF()` e `classificarRiscoManchester()` existiam em `helpers.php` mas nunca eram chamados; agora são obrigatórios na API.
- O leito podia ser marcado "Ocupado" manualmente sem paciente internado (RN-LEI-03).
- "Pacientes internados" contava internações sem data de alta, incluindo transferências e status inconsistentes (RN-INT-06).
- "Recebido (Mês)" somava todos os pagamentos de todos os meses (RN-FAT-07).
- A consolidação de gastos somava todo insumo já usado no leito, inclusive por outros pacientes (RN-FAT-02 agora considera só o período da internação do paciente).
- A fila de atendimento listava o mesmo paciente várias vezes, porque ligava triagem e consulta pelo paciente; agora a triagem fica vinculada à consulta (RN-TRI-01).
- A guia de faturamento podia ser criada já como "Pago" (RN-FAT-04).
