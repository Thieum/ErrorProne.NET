﻿using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace ErrorProne.NET.Core;

[Flags]
public enum TaskLikeTypes
{
    Task = 1 << 0,
    TaskOfT = 1 << 1,
    ValueTask = 1 << 2,
    ValueTaskOfT = 1 << 3,
    TasksOnly = Task | TaskOfT,
    All = Task | TaskOfT | ValueTask
}

public record TaskLikeTypesHolder(
        INamedTypeSymbol? TaskType,
        INamedTypeSymbol? TaskOfTType,
        INamedTypeSymbol? ValueTaskType,
        INamedTypeSymbol? ValueTaskOfTType);

public static class TaskTypeExtensions
{
    public static TaskLikeTypesHolder GetTaskTypes(Compilation compilation)
    {
        var taskType = compilation.TaskType();
        var taskOfTType = compilation.TaskOfTType();
        var valueTaskType = compilation.ValueTaskType();
        var valueTaskOfTType = compilation.ValueTaskOfTType();

        return new(taskType, taskOfTType, valueTaskType, valueTaskOfTType);
    }

    public static bool IsTaskLike(this ITypeSymbol? type, Compilation compilation, TaskLikeTypes typesToCheck)
    {
        if (type == null)
        {
            return false;
        }

        var (taskType, taskOfTType, valueTaskType, valueTaskOfTType) = GetTaskTypes(compilation);
        if (taskType == null || taskOfTType == null)
        {
            return false; // ?
        }

        if ((typesToCheck & TaskLikeTypes.Task) != 0 && type.Equals(taskType, SymbolEqualityComparer.Default))
        {
            return true;
        }

        if ((typesToCheck & TaskLikeTypes.TaskOfT) != 0 && type.OriginalDefinition.Equals(taskOfTType, SymbolEqualityComparer.Default))
        {
            return true;
        }

        if ((typesToCheck & TaskLikeTypes.ValueTask) != 0 && type.Equals(valueTaskType, SymbolEqualityComparer.Default))
        {
            return true;
        }
        
        if ((typesToCheck & TaskLikeTypes.ValueTaskOfT) != 0 && type.OriginalDefinition.Equals(valueTaskOfTType, SymbolEqualityComparer.Default))
        {
            return true;
        }

        if (type.IsErrorType())
        {
            return type.Name.Equals("Task") ||
                   type.Name.Equals("ValueTask");
        }

        return false;
    }

    public static bool IsTaskLike(this ITypeSymbol? type, Compilation compilation) =>
        IsTaskLike(type, compilation, TaskLikeTypes.All);

    public static bool IsTaskCompletionSource(this ITypeSymbol? type, Compilation compilation)
    {
        if (type == null)
        {
            return false;
        }

        return type.IsClrType(compilation, typeof(TaskCompletionSource<>)) ||
               // A non-generic version is not available in .netstandard2.0
               // so using the full type name here.
               type.OriginalDefinition.Equals(
                   compilation.GetTypeByFullName("System.Threading.Tasks.TaskCompletionSource"),
                   SymbolEqualityComparer.Default);
    }
}