# Optimize — regras obrigatórias do produto

## 1. Português do Brasil é o idioma padrão

Toda informação destinada ao usuário deve aparecer em pt-BR:

- navegação;
- títulos e descrições;
- botões;
- estados e mensagens de progresso;
- erros e confirmações;
- recomendações;
- avisos de risco;
- missões;
- explicações de serviços/tweaks;
- relatórios gerados pelo aplicativo.

Termos técnicos reconhecidos (CPU, GPU, BIOS, DNS, NVIDIA, AMD, Intel, Windows Update etc.) podem ser mantidos quando traduzir reduzir clareza.

A CI gera `ptbr-audit.txt` com possíveis textos visíveis ainda em inglês. Antes de considerar a migração concluída, a auditoria deve entrar em modo estrito e ficar limpa, exceto pela lista técnica permitida.

## 2. O usuário não precisa entender ferramentas de sistema

O Optimize não deve presumir que o usuário sabe o que são timer resolution, affinity, standby list, telemetry, services ou power plans.

A experiência principal é:

1. entender o computador;
2. encontrar oportunidades/problemas;
3. apresentar missões em linguagem simples;
4. explicar por que aquilo importa naquele PC;
5. mostrar evidência;
6. indicar risco e necessidade de administrador;
7. permitir corrigir somente quando a compatibilidade foi comprovada;
8. medir novamente e mostrar antes x depois.

As ferramentas técnicas continuam disponíveis, mas deixam de ser a única forma de usar o aplicativo.

## 3. Nenhuma otimização é genérica

Toda ação que modifica o computador precisa considerar, quando relevante:

- fabricante/modelo da CPU;
- fabricante/modelo da GPU e driver;
- múltiplas GPUs/hardware híbrido;
- placa-mãe e BIOS;
- RAM, módulos e frequências;
- tipo de armazenamento;
- notebook x desktop;
- Windows/build;
- temperatura e estado atual;
- contexto (jogo, uso normal, bateria/energia quando aplicável).

O Optimize trabalha por capacidade detectada, não por uma lista rígida de SKUs. Isso permite atender novas gerações sem assumir que dois modelos do mesmo fabricante suportam exatamente os mesmos recursos.

## 4. Compatibilidade falha de forma segura

Regra padrão: **ação desconhecida = bloqueada**.

- tweak NVIDIA sem GPU NVIDIA: bloqueado;
- tweak AMD sem GPU AMD: bloqueado;
- tweak Intel sem GPU Intel: bloqueado;
- capacidade que não foi comprovada: não executar automaticamente;
- XMP/EXPO/Resizable BAR/SAM/ajustes de BIOS: orientação até existir um método seguro de validação e aplicação;
- notebooks recebem regras de energia/temperatura diferentes de desktops;
- ações de jogo devem ser reversíveis e, quando possível, limitadas à sessão/processo do jogo.

`OptimizationCompatibilityService` é o gate central. Novos executores não devem contorná-lo.

## 5. Alterações precisam ser explicáveis e reversíveis

Antes de uma ação invasiva:

- capturar estado anterior;
- criar ponto de restauração quando fizer sentido;
- informar exatamente o que será alterado;
- registrar resultado;
- oferecer desfazer;
- restaurar automaticamente quando a ação for temporária (ex.: perfil de jogo).

A IA, se adicionada, poderá classificar/recomendar ações cadastradas. Ela não poderá inventar PowerShell/registro e executar como administrador.

## 6. Medir antes e depois

Uma otimização só deve ser vendida como melhoria quando houver uma métrica relacionada que possa ser comparada, por exemplo:

- processos em segundo plano;
- RAM ociosa/em uso;
- tempo de inicialização;
- espaço em disco;
- temperaturas;
- clocks/limitação térmica quando disponível;
- latência/rede quando a ação for de rede;
- FPS/frame time quando houver benchmark confiável.

O Optimize não promete ganhos arbitrários de FPS ou “100% mais rápido”.
