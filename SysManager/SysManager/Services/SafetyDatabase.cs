// SysManager · SafetyDatabase — curated safety ratings for services and features
// Author: laurentiu021 · https://github.com/laurentiu021/SystemManager
// License: MIT

using System.Collections.Frozen;
using SysManager.Models;

namespace SysManager.Services;

public static class SafetyDatabase
{
    public static (SafetyLevel Level, string Description) GetServiceSafety(string serviceName)
    {
        if (SafeServices.TryGetValue(serviceName, out var safe))
            return (SafetyLevel.Safe, safe);
        if (CautionServices.TryGetValue(serviceName, out var caution))
            return (SafetyLevel.Caution, caution);
        if (CriticalServices.TryGetValue(serviceName, out var critical))
            return (SafetyLevel.Critical, critical);
        return (SafetyLevel.Critical, "Serviço desconhecido — trate como crítico até que a compatibilidade seja verificada.");
    }

    public static (SafetyLevel Level, string Description) GetFeatureSafety(string featureName)
    {
        if (SafeFeatures.TryGetValue(featureName, out var safe))
            return (SafetyLevel.Safe, safe);
        if (CautionFeatures.TryGetValue(featureName, out var caution))
            return (SafetyLevel.Caution, caution);
        if (CriticalFeatures.TryGetValue(featureName, out var critical))
            return (SafetyLevel.Critical, critical);
        return (SafetyLevel.Caution, "Verifique a documentação e o uso deste computador antes de modificar este recurso.");
    }

    private static readonly FrozenDictionary<string, string> SafeServices = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["DiagTrack"] = "Experiências do Usuário Conectado e Telemetria — envia dados de diagnóstico à Microsoft. Desativar interrompe essa telemetria, mas pode reduzir informações de diagnóstico.",
        ["dmwappushservice"] = "Roteamento de mensagens WAP Push — usado principalmente por recursos de telemetria e mensagens do sistema. Normalmente dispensável em PCs pessoais, mas o Optimize deve confirmar o contexto antes de alterar.",
        ["SysMain"] = "SysMain mantém dados de aplicativos em cache para acelerar carregamentos. O benefício varia conforme RAM, armazenamento e padrão de uso; não deve ser desativado automaticamente só porque o PC usa SSD.",
        ["WSearch"] = "Windows Search indexa arquivos para acelerar pesquisas. Só faz sentido desativar ou pausar quando o usuário não depende da pesquisa do Explorer/Start ou durante uma sessão temporária e reversível.",
        ["MapsBroker"] = "Gerenciador de Mapas Baixados — cuida de mapas offline e atualizações relacionadas. Pode ser dispensável quando nenhum recurso de mapas é usado.",
        ["lfsvc"] = "Serviço de Geolocalização — fornece localização para Windows e aplicativos. Desativar pode aumentar privacidade, mas quebra apps que dependem de localização.",
        ["RetailDemo"] = "Serviço de Demonstração de Varejo — destinado a computadores de exposição em lojas. Em um PC pessoal comum, normalmente pode ficar desativado.",
        ["wisvc"] = "Serviço Windows Insider — necessário para recursos do Programa Windows Insider. Não deve ser removido se o PC participa do programa.",
        ["TabletInputService"] = "Teclado virtual e manuscrito — necessário em dispositivos com toque, caneta ou alguns recursos de acessibilidade. O hardware precisa ser considerado antes de desativar.",
        ["Fax"] = "Fax do Windows — necessário apenas quando o computador envia/recebe fax por hardware ou integração compatível.",
        ["XblAuthManager"] = "Gerenciador de autenticação do Xbox Live — usado por recursos Xbox, Game Pass e jogos que dependem desses serviços.",
        ["XblGameSave"] = "Xbox Live Game Save — usado por jogos com salvamento e sincronização ligados aos serviços Xbox.",
        ["XboxGipSvc"] = "Gerenciamento de acessórios Xbox — pode ser necessário para controles e acessórios Xbox, dependendo de como estão conectados.",
        ["XboxNetApiSvc"] = "Rede do Xbox Live — usado por recursos online/multiplayer de jogos e serviços Xbox.",
        ["WMPNetworkSvc"] = "Compartilhamento de rede do Windows Media Player — recurso legado de compartilhamento de mídia, pouco usado em PCs modernos.",
        ["AxInstSV"] = "Instalador ActiveX — componente legado ligado a tecnologias antigas. Normalmente não é necessário para navegadores modernos.",
        ["RemoteRegistry"] = "Registro Remoto — permite alterar o Registro pela rede. Em PCs pessoais costuma ser desnecessário e aumenta a superfície de ataque quando habilitado.",
        ["TrkWks"] = "Cliente de Rastreamento de Link Distribuído — mantém referências de arquivos NTFS movidos em determinados cenários de rede/domínio. Pouco necessário em uso doméstico.",
        ["WerSvc"] = "Relatório de Erros do Windows — coleta e envia informações de falhas para ajudar no diagnóstico. Desativar reduz relatórios, mas também pode dificultar análise de problemas.",
        ["PhoneSvc"] = "Serviço de Telefonia/Telefone — usado por integrações e recursos ligados a chamadas/dispositivos móveis. Pode ser dispensável em muitos desktops.",
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<string, string> CautionServices = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["wuauserv"] = "Windows Update — gerencia atualizações e correções de segurança. Desativar pode impedir atualizações importantes; no máximo deve ser pausado de forma temporária e consciente.",
        ["Spooler"] = "Spooler de Impressão — necessário para impressão e para alguns softwares que usam impressoras virtuais. Só desative quando o usuário realmente não utiliza impressão.",
        ["BITS"] = "Serviço de Transferência Inteligente em Segundo Plano — usado por Windows Update, Microsoft Store e outros componentes. Desativar pode quebrar downloads e atualizações.",
        ["Themes"] = "Temas — fornece recursos visuais do Windows. Desativar altera a aparência e costuma gerar economia mínima de recursos.",
        ["AudioSrv"] = "Áudio do Windows — necessário para reprodução e captura de som. Desativar remove o áudio do sistema e não é uma otimização aceitável para um PC comum.",
        ["Dhcp"] = "Cliente DHCP — obtém automaticamente endereço IP e parâmetros de rede. Só pode ser desativado quando existe configuração de IP estático validada.",
        ["Dnscache"] = "Cliente DNS — mantém cache de consultas DNS e participa da resolução de nomes. Alterações podem afetar navegação e conectividade.",
        ["EventLog"] = "Log de Eventos do Windows — essencial para diagnóstico, auditoria e funcionamento de diversos componentes. Desativar prejudica solução de problemas.",
        ["LanmanServer"] = "Servidor — oferece compartilhamento SMB deste computador para outros dispositivos. Só desative após confirmar que nenhum compartilhamento/recurso depende dele.",
        ["LanmanWorkstation"] = "Estação de Trabalho — cliente SMB usado para acessar compartilhamentos de rede. Desativar impede acesso normal a pastas e recursos SMB.",
        ["Schedule"] = "Agendador de Tarefas — muitos componentes do Windows e aplicativos dependem dele. Desativar pode quebrar manutenção, atualizações e tarefas essenciais.",
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<string, string> CriticalServices = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["RpcSs"] = "Chamada de Procedimento Remoto (RPC) — infraestrutura central de comunicação do Windows. Não desative.",
        ["RpcEptMapper"] = "Mapeador de Ponto de Extremidade RPC — necessário para RPC e vários componentes do sistema. Não desative.",
        ["DcomLaunch"] = "Inicializador de Processos de Servidor DCOM — componente central do Windows. Alterá-lo pode impedir o sistema de funcionar corretamente.",
        ["LSM"] = "Gerenciador de Sessão Local — gerencia sessões de usuário e login. É crítico para o Windows.",
        ["SamSs"] = "Gerenciador de Contas de Segurança — participa do armazenamento e gerenciamento de informações de segurança/autenticação. Não desative.",
        ["WinDefend"] = "Microsoft Defender Antivirus — camada principal de proteção antimalware do Windows. O Optimize não deve desativá-lo como 'otimização'.",
        ["mpssvc"] = "Firewall do Microsoft Defender — proteção de rede do Windows. Desativá-lo expõe o computador e não é uma otimização aceitável.",
        ["BFE"] = "Mecanismo de Filtragem Base — núcleo usado pelo Firewall e filtros de rede. É um serviço crítico.",
        ["CryptSvc"] = "Serviços de Criptografia — cuida de certificados, assinaturas e partes do Windows Update. Alterações podem quebrar atualizações e validações de segurança.",
        ["lsass"] = "Autoridade de Segurança Local — núcleo de autenticação e segurança do Windows. Não deve ser alterado ou encerrado.",
        ["Winmgmt"] = "Instrumentação de Gerenciamento do Windows (WMI) — usada pelo próprio Optimize e por diversas ferramentas do sistema. Não desative.",
        ["PlugPlay"] = "Plug and Play — detecta e gerencia dispositivos. Desativar quebra reconhecimento e funcionamento de hardware.",
        ["Power"] = "Energia — gerencia políticas e eventos de energia. Alterações indevidas podem causar comportamento imprevisível.",
        ["ProfSvc"] = "Serviço de Perfil de Usuário — carrega perfis de usuário. Desativar pode impedir login e acesso correto ao perfil.",
        ["nsi"] = "Interface de Armazenamento de Rede — componente central da conectividade de rede do Windows. Não desative.",
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<string, string> SafeFeatures = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Internet-Explorer-Optional-amd64"] = "Compatibilidade legada do Internet Explorer/Edge. Pode ser dispensável, exceto quando sites corporativos antigos dependem desse modo.",
        ["MediaPlayback"] = "Recursos legados de reprodução de mídia. Alguns programas antigos podem depender deles; confirme o uso antes de remover.",
        ["WindowsMediaPlayer"] = "Componentes legados do Windows Media Player. Aplicativos antigos ou codecs específicos podem depender deles.",
        ["Printing-XPSServices-Features"] = "Microsoft XPS Document Writer — impressora virtual pouco usada, mas necessária em alguns fluxos de documentos XPS.",
        ["Printing-PrintToPDFServices-Features"] = "Microsoft Print to PDF — impressora virtual útil para salvar documentos como PDF. Remover economiza pouco e pode reduzir funcionalidade.",
        ["WorkFolders-Client"] = "Pastas de Trabalho — sincronização corporativa de arquivos. Normalmente dispensável fora de ambientes empresariais que usam o recurso.",
        ["MicrosoftWindowsPowerShellV2Root"] = "PowerShell 2.0 — versão legada e insegura. Softwares corporativos muito antigos podem depender dela, mas em sistemas modernos é preferível manter versões atuais.",
        ["MicrosoftWindowsPowerShellV2"] = "Mecanismo PowerShell 2.0 — tecnologia legada. Remover reduz superfície de ataque quando nenhum software antigo depende dela.",
        ["MSRDC-Infrastructure"] = "Infraestrutura de cliente de Área de Trabalho Remota — necessária para determinados cenários de conexão remota.",
        ["TelnetClient"] = "Cliente Telnet — protocolo legado sem criptografia. Só mantenha se existir necessidade específica; SSH é preferível.",
        ["TFTP"] = "Cliente TFTP — transferência simples de arquivos usada em equipamentos/rede específicos. Pouco necessária em PCs comuns.",
        ["DirectPlay"] = "DirectPlay — API legada de jogos, necessária para alguns títulos antigos. Não remova se o usuário joga títulos que dependem dela.",
        ["SimpleTCP"] = "Serviços TCP/IP simples — recursos legados como echo/daytime. Raramente necessários em estações de trabalho.",
        ["SMB1Protocol"] = "SMB 1.0 — protocolo antigo e inseguro. Deve permanecer desativado salvo quando um equipamento legado indispensável exige SMB1.",
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<string, string> CautionFeatures = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["NetFx4-AdvSrvs"] = ".NET Framework 4.x Advanced Services — alguns programas dependem desses componentes. Não remova sem validar os aplicativos instalados.",
        ["NetFx3"] = ".NET Framework 3.5 — necessário para vários programas e jogos mais antigos. Remover pode impedir que eles abram.",
        ["SearchEngine-Client-Package"] = "Windows Search — fornece pesquisa no menu Iniciar e Explorer. Desativar remove ou degrada esses recursos.",
        ["Printing-Foundation-Features"] = "Serviços de Impressão e Documentos — necessários para impressão física e várias impressoras virtuais.",
        ["SmbDirect"] = "SMB Direct (RDMA) — transferência de arquivos de alto desempenho em hardware de rede compatível. Só é dispensável quando não existe uso de RDMA.",
        ["WCF-Services45"] = "Serviços WCF — infraestrutura de comunicação do .NET usada por alguns aplicativos corporativos. Remover pode quebrar esses programas.",
        ["Microsoft-Windows-Subsystem-Linux"] = "Subsistema do Windows para Linux (WSL) — necessário para usuários e ferramentas que executam ambientes Linux no Windows.",
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<string, string> CriticalFeatures = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Microsoft-Hyper-V-All"] = "Hyper-V — plataforma de virtualização usada por VMs e ferramentas como Docker/WSL2 em determinadas configurações. Não desative sem entender as dependências deste PC.",
        ["Microsoft-Hyper-V"] = "Núcleo do Hyper-V — desativar interrompe workloads e ferramentas que dependem do hipervisor.",
        ["VirtualMachinePlatform"] = "Plataforma de Máquina Virtual — necessária para WSL2 e outros recursos de virtualização do Windows.",
        ["HypervisorPlatform"] = "Plataforma do Hipervisor do Windows — usada por virtualizadores e ferramentas de terceiros. Desativar pode quebrar VMs/emuladores.",
        ["Containers"] = "Contêineres do Windows — usados por Docker e workloads de contêiner. Desativar quebra esses cenários quando estão em uso.",
        ["Microsoft-Windows-Client-EmbeddedExp-Package"] = "Windows Sandbox — ambiente isolado de testes e segurança. Remover elimina esse recurso quando ele é utilizado.",
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
}
