// Optimize · mandatory hardware compatibility gate
// Based on SysManager (MIT) — original license preserved in repository.

using SysManager.Models;

namespace SysManager.Services;

/// <summary>
/// Central safety policy for any optimization that may modify Windows/hardware behavior.
/// Unknown actions are blocked by default. An executor must receive a positive decision
/// from this service before touching the machine.
/// </summary>
public sealed class OptimizationCompatibilityService
{
    public const string NvidiaProfile = "gpu.nvidia.profile";
    public const string AmdProfile = "gpu.amd.profile";
    public const string IntelProfile = "gpu.intel.profile";
    public const string UltimatePerformance = "power.ultimate-performance";
    public const string HighPerformance = "power.high-performance";
    public const string BalancedPower = "power.balanced";
    public const string ProcessorMinState = "power.processor-min-state";
    public const string Hibernation = "power.hibernation";
    public const string VisualEffects = "windows.visual-effects";
    public const string WindowsGameMode = "windows.game-mode";
    public const string XboxGameBar = "windows.xbox-game-bar";
    public const string CpuAffinity = "gaming.cpu-affinity";
    public const string CpuPriority = "gaming.cpu-priority";
    public const string TimerResolution = "gaming.timer-resolution";
    public const string StandbyMemoryPurge = "memory.standby-purge";
    public const string WorkingSetTrim = "memory.working-set-trim";
    public const string PauseSearchIndexing = "windows.search-pause";
    public const string SilenceNotifications = "gaming.silence-notifications";
    public const string Xmp = "bios.xmp";
    public const string Expo = "bios.expo";
    public const string ResizableBar = "bios.resizable-bar";

    public OptimizationCompatibility Evaluate(
        string optimizationId,
        HardwareProfile hardware,
        bool? runningOnBattery = null)
    {
        var context = $"{hardware.CpuName} · {hardware.GpuSummary}";

        if (optimizationId.StartsWith("gpu.nvidia.", StringComparison.OrdinalIgnoreCase))
            return VendorGate(optimizationId, hardware.HasNvidiaGpu, "NVIDIA", context);

        if (optimizationId.StartsWith("gpu.amd.", StringComparison.OrdinalIgnoreCase))
            return VendorGate(optimizationId, hardware.HasAmdGpu, "AMD Radeon", context);

        if (optimizationId.StartsWith("gpu.intel.", StringComparison.OrdinalIgnoreCase))
            return VendorGate(optimizationId, hardware.HasIntelGpu, "Intel", context);

        return optimizationId switch
        {
            Xmp => Guidance(optimizationId,
                "XMP depende do processador, placa-mãe, BIOS e kit de memória. O Optimize pode orientar e validar sinais de compatibilidade, mas não deve habilitar firmware automaticamente.", context),

            Expo => Guidance(optimizationId,
                hardware.CpuVendor == CpuVendor.Amd
                    ? "EXPO é voltado a plataformas AMD compatíveis, mas o suporte real depende da placa-mãe, BIOS e memória. Somente orientação."
                    : "EXPO é voltado a plataformas AMD compatíveis. Este hardware não fornece evidência suficiente para alteração automática.", context),

            ResizableBar => Guidance(optimizationId,
                "Resizable BAR/SAM depende simultaneamente de CPU, placa-mãe, BIOS, GPU, VBIOS e configuração de firmware. Só será recomendado após confirmação de capacidade; nunca ativado às cegas.", context),

            UltimatePerformance => EvaluateAggressivePowerPlan(optimizationId, hardware, runningOnBattery, context),

            HighPerformance => EvaluateAggressivePowerPlan(optimizationId, hardware, runningOnBattery, context),

            BalancedPower => new OptimizationCompatibility(
                optimizationId, CompatibilityState.Supported,
                "O plano Equilibrado é uma opção segura para uso geral e é especialmente adequado para notebooks quando não existe uma necessidade específica de desempenho máximo.", context),

            ProcessorMinState => EvaluateProcessorState(optimizationId, hardware, runningOnBattery, context),

            Hibernation => new OptimizationCompatibility(
                optimizationId, CompatibilityState.RequiresConfirmation,
                hardware.IsLaptop
                    ? "Hibernação é útil em notebooks e desativá-la também pode afetar a Inicialização Rápida em algumas configurações. Alterar somente por escolha explícita do usuário."
                    : "Hibernação não é uma otimização de desempenho. Alterar somente por escolha explícita do usuário e informar o impacto no arquivo hiberfil.sys e na Inicialização Rápida.", context),

            VisualEffects => new OptimizationCompatibility(
                optimizationId, CompatibilityState.RequiresConfirmation,
                "A redução de efeitos visuais é reversível, mas muda a experiência do Windows. O estado atual deve ser salvo antes da alteração.", context),

            WindowsGameMode => new OptimizationCompatibility(
                optimizationId, CompatibilityState.RequiresConfirmation,
                "O Modo de Jogo é um recurso nativo do Windows. O Optimize pode alterar seu estado de forma reversível, mas não assume que ligado ou desligado é melhor para todos os jogos.", context),

            XboxGameBar => new OptimizationCompatibility(
                optimizationId, CompatibilityState.RequiresConfirmation,
                "Desativar a Xbox Game Bar pode reduzir sobreposições/captura em segundo plano, mas remove recursos usados por alguns jogadores. O estado original deve ser preservado para desfazer.", context),

            CpuAffinity when hardware.CpuThreads >= 2 => new OptimizationCompatibility(
                optimizationId, CompatibilityState.RequiresConfirmation,
                "Afinidade de CPU só deve ser aplicada por processo/jogo e sempre com a máscara original salva para reversão.", context),

            CpuAffinity => new OptimizationCompatibility(
                optimizationId, CompatibilityState.Blocked,
                "Não há topologia de CPU suficiente para definir uma afinidade segura.", context),

            CpuPriority => new OptimizationCompatibility(
                optimizationId, CompatibilityState.RequiresConfirmation,
                "Prioridade de CPU pode favorecer o jogo, mas deve ser aplicada somente ao processo escolhido e restaurada ao final da sessão.", context),

            TimerResolution => new OptimizationCompatibility(
                optimizationId, CompatibilityState.RequiresConfirmation,
                "Resolução de temporizador é permitida apenas em sessão de jogo, com reversão automática ao encerrar o processo ou o perfil.", context),

            StandbyMemoryPurge => new OptimizationCompatibility(
                optimizationId, CompatibilityState.RequiresConfirmation,
                "A limpeza da lista standby é temporária e não deve rodar continuamente. Pode ser usada de forma pontual e nunca como 'limpeza de RAM' permanente.", context),

            WorkingSetTrim => new OptimizationCompatibility(
                optimizationId, CompatibilityState.GuidanceOnly,
                "Esvaziar o working set de todos os processos pode apenas deslocar páginas de memória e causar novas leituras/soft faults depois. O Optimize não oferece isso como otimização automática.", context),

            PauseSearchIndexing => new OptimizationCompatibility(
                optimizationId, CompatibilityState.RequiresConfirmation,
                "A indexação pode ser pausada apenas temporariamente durante a sessão, preservando se o serviço já estava parado e restaurando o estado anterior depois.", context),

            SilenceNotifications => new OptimizationCompatibility(
                optimizationId, CompatibilityState.RequiresConfirmation,
                "Notificações podem ser silenciadas durante o jogo se o valor anterior for salvo e restaurado ao encerrar a sessão.", context),

            _ => new OptimizationCompatibility(
                optimizationId, CompatibilityState.Blocked,
                "Esta otimização ainda não possui uma regra de compatibilidade auditada no Optimize e foi bloqueada por segurança.", context)
        };
    }

    private static OptimizationCompatibility EvaluateAggressivePowerPlan(
        string id,
        HardwareProfile hardware,
        bool? runningOnBattery,
        string context)
    {
        if (hardware.IsLaptop && runningOnBattery == true)
            return new OptimizationCompatibility(id, CompatibilityState.Blocked,
                "Plano de alto desempenho foi bloqueado porque o notebook está usando bateria. Isso aumentaria consumo e temperatura sem uma fonte de energia conectada.", context);

        if (hardware.IsLaptop)
            return new OptimizationCompatibility(id, CompatibilityState.RequiresConfirmation,
                "Em notebook, planos de alto desempenho aumentam consumo, temperatura e ruído. Só aplicar conectado à energia e com o plano anterior salvo para reversão.", context);

        return new OptimizationCompatibility(id, CompatibilityState.RequiresConfirmation,
            "O plano de alto desempenho pode ser usado neste desktop, mas o plano atual precisa ser salvo antes da troca para permitir desfazer.", context);
    }

    private static OptimizationCompatibility EvaluateProcessorState(
        string id,
        HardwareProfile hardware,
        bool? runningOnBattery,
        string context)
    {
        if (hardware.IsLaptop && runningOnBattery == true)
            return new OptimizationCompatibility(id, CompatibilityState.Blocked,
                "Forçar estado mínimo alto da CPU em um notebook na bateria foi bloqueado por consumo, temperatura e autonomia.", context);

        return new OptimizationCompatibility(id, CompatibilityState.RequiresConfirmation,
            hardware.IsLaptop
                ? "Alterar o estado mínimo da CPU em notebook afeta consumo e temperatura. Só aplicar conectado à energia e com o valor original salvo."
                : "Alterar o estado mínimo da CPU pode aumentar consumo e temperatura sem melhorar todos os workloads. O valor original deve ser salvo para reversão.", context);
    }

    private static OptimizationCompatibility VendorGate(string id, bool detected, string vendor, string context)
        => detected
            ? new OptimizationCompatibility(id, CompatibilityState.RequiresConfirmation,
                $"Hardware {vendor} detectado. A ação ainda precisa validar recurso/driver específico e criar estado de reversão antes de executar.", context)
            : new OptimizationCompatibility(id, CompatibilityState.Blocked,
                $"Ação exclusiva de {vendor} bloqueada porque nenhum hardware compatível desse fabricante foi detectado.", context);

    private static OptimizationCompatibility Guidance(string id, string reason, string context)
        => new(id, CompatibilityState.GuidanceOnly, reason, context);
}
