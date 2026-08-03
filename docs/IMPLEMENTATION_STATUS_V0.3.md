# Optimize v0.3 — estado da implementação

Esta versão transforma a base SystemManager em uma fundação do Optimize orientada a diagnóstico e segurança.

## Implementado

- produto/assembly renomeado para `Optimize.exe`;
- interface pt-BR com auditoria automatizada;
- perfil de hardware sem coleta de serial/UUID;
- detecção de CPU, placa-mãe, BIOS, RAM, armazenamento, notebook/desktop e GPUs;
- classificação de GPU NVIDIA, AMD Radeon e Intel;
- telemetria de carga/temperatura de GPU via LibreHardwareMonitor quando o sensor existe;
- fallback seguro: valor indisponível fica indisponível, nunca é inventado;
- motor de Missões do Optimize com prioridade, risco, evidência, contexto de hardware e próxima ação;
- Missões promovidas para o topo do Dashboard;
- `OptimizationCompatibilityService` com política fail-closed (`ação desconhecida = bloqueada`);
- XMP/EXPO/Resizable BAR/SAM tratados como orientação, não automação;
- regras de energia diferentes para notebook/desktop e bateria/AC;
- Perfil para jogos roteado por gate de compatibilidade antes do motor reversível original;
- ponto de restauração/snapshot/reversão/crash recovery herdados do motor de perfil para jogos;
- telas legadas de tweaks ainda não migradas ficam somente leitura em vez de aplicar ajustes genéricos;
- descrições de segurança de serviços/recursos revisadas e traduzidas;
- diálogos e mensagens dinâmicas passam pela localização pt-BR.

## Política de entrega

A pipeline `Validate Optimize` executa a auditoria pt-BR em modo estrito, compila no Windows e publica um executável x64 autossuficiente. Novo texto de interface em inglês deve falhar a validação em vez de entrar silenciosamente no produto.

## Próximas migrações

As ferramentas herdadas continuam sendo migradas individualmente para o gate de compatibilidade. Uma ação só deixa o modo somente leitura quando possui contexto necessário, risco conhecido, confirmação adequada e estratégia de reversão quando aplicável.
