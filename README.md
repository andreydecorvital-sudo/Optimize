# Optimize

Aplicativo Windows para diagnosticar gargalos, explicar problemas e aplicar otimizações seguras e reversíveis.

## Objetivo do MVP

- escanear Windows, CPU, memória, armazenamento, processos e inicialização;
- atribuir uma pontuação de saúde/desempenho;
- apresentar recomendações em linguagem simples;
- nunca aplicar mudanças sem consentimento;
- preparar uma camada de segurança com registro e reversão das alterações.

## Stack inicial

- C# e .NET 8;
- WPF para o aplicativo Windows;
- arquitetura separada entre interface e motor de diagnóstico;
- GitHub Actions para validação automática.

## Estado

Projeto em desenvolvimento. A primeira etapa implementa um scanner local somente leitura e um dashboard funcional.

## Segurança

O Optimize não deve usar limpadores de registro genéricos, desativar recursos de segurança silenciosamente ou aplicar pacotes de tweaks sem explicar cada alteração.
