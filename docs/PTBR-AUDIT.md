# Auditoria pt-BR do Optimize

A primeira auditoria de idioma analisava apenas literais encontrados em arquivos XAML/C#. Ela foi útil para a migração inicial, mas não representava toda a interface porque não enxergava corretamente:

- textos produzidos por bindings e enums;
- menus de contexto, tooltips e pop-ups em árvores WPF desconectadas;
- placeholders e propriedades de controles de terceiros;
- textos carregados depois da abertura da página;
- nomes de acessibilidade e botões de diálogos;
- estados vazios e mensagens produzidas em tempo de execução.

## Auditoria atual

A pipeline `Validate Optimize` executa duas verificações:

1. `tools/ptbr_audit.py --strict` fiscaliza textos visíveis encontrados no código-fonte.
2. `PtBrSurfaceUiTests` abre o `Optimize.exe` no Windows, expande a navegação, visita as 58 telas registradas e lê o relatório produzido pelo tradutor em tempo real.

O relatório de execução é gravado em:

`%LOCALAPPDATA%\Optimize\ptbr-live-audit.log`

A build falha quando a interface renderizada ainda contém uma mensagem classificada como inglês.

## Resultado atual

Na execução aprovada da versão v0.4:

- auditoria estática pt-BR: aprovada;
- build do aplicativo: aprovada;
- build da auditoria de interface: aprovada;
- navegação automatizada pelas 58 telas: aprovada;
- auditoria da interface renderizada: aprovada;
- publicação Windows x64 autossuficiente: aprovada.

Essa verificação não substitui testes em diferentes versões do Windows, fabricantes de hardware e dados reais de cada usuário. Textos fornecidos pelo próprio driver, firmware, Windows ou aplicativo de terceiros podem permanecer no idioma de origem e devem ser classificados como conteúdo técnico externo, não como interface do Optimize.
