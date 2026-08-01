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
    public const string CpuAffinity = "gaming.cpu-affinity";
    public const string TimerResolution = "gaming.timer-resolution";
    public const string StandbyMemoryPurge = "memory.standby-purge";
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
                hardware.CpuVendor == CpuVendor.Amd
                    ? "XMP depende da placa-mãe, BIOS e do kit de memória. O Optimize pode orientar, mas não deve habilitar isso automaticamente."
                    : "XMP é uma configuração de firmware/memória. O Optimize pode orientar, mas não deve habilitar isso automaticamente.", context),

            Expo => Guidance(optimizationId,
                hardware.CpuVendor == CpuVendor.Amd
                    ? "EXPO é compatível com plataformas AMD específicas, mas suporte real depende da placa-mãe, BIOS e memória. Somente orientação."
                    : "EXPO é voltado a plataformas AMD compatíveis. Este hardware não fornece evidência suficiente para alteração automática.", context),

            ResizableBar => Guidance(optimizationId,
                "Resizable BAR/SAM depende simultaneamente de CPU, placa-mãe, BIOS, GPU, VBIOS e configuração de firmware. Só será recomendado após confirmação de capacidade; nunca ativado às cegas.", context),

            UltimatePerformance => EvaluatePowerPlan(optimizationId, hardware, runningOnBattery, context),

            CpuAffinity when hardware.CpuThreads >= 2 => new OptimizationCompatibility(
                optimizationId, CompatibilityState.RequiresConfirmation,
                "Afinidade de CPU só deve ser aplicada por processo/jogo e sempre com o valor original salvo para reversão.", context),

            TimerResolution => new OptimizationCompatibility(
                optimizationId, CompatibilityState.RequiresConfirmation,
                "Resolução de temporizador é permitida apenas em sessão de jogo, com reversão automática ao encerrar o processo.", context),

            StandbyMemoryPurge => new OptimizationCompatibility(
                optimizationId, CompatibilityState.RequiresConfirmation,
                "A limpeza da lista standby é temporária e não deve rodar continuamente. Pode ser usada de forma pontual e reversível.", context),

            _ => new OptimizationCompatibility(
                optimizationId, CompatibilityState.Blocked,
                "Esta otimização ainda não possui uma regra de compatibilidade auditada no Optimize e foi bloqueada por segurança.", context)
        };
    }

    private static OptimizationCompatibility EvaluatePowerPlan(
        string id,
        HardwareProfile hardware,
        bool? runningOnBattery,
        string context)
    {
        if (!hardware.IsLaptop)
            return new OptimizationCompatibility(id, CompatibilityState.RequiresConfirmation,
                "Perfil de alto desempenho é suportado, mas o plano atual precisa ser salvo antes da troca para permitir desfazer.", context);

        if (runningOnBattery == true)
            return new OptimizationCompatibility(id, CompatibilityState.Blocked,
                "Perfil de desempenho máximo foi bloqueado porque o notebook está usando bateria.", context);

        return new OptimizationCompatibility(id, CompatibilityState.RequiresConfirmation,
            "Em notebook, desempenho máximo aumenta consumo e temperatura. Só aplicar conectado à energia e após confirmação do usuário.", context);
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
