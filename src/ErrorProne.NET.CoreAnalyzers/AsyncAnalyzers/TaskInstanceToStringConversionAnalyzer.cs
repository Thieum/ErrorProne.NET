﻿using ErrorProne.NET.Core;
using ErrorProne.NET.CoreAnalyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ErrorProne.NET.AsyncAnalyzers
{
    /// <summary>
    /// The analyzer warns when a task instance is implicitly converted to string.
    /// </summary>
    /// <remarks>
    /// Here is an example: <code>var r = FooAsync(); Console.WriteLine($"r: {r}");</code>
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TaskInstanceToStringConversionAnalyzer : DiagnosticAnalyzerBase
    {
        /// <nodoc />
        public static string DiagnosticId => Rule.Id;
        
        /// <nodoc />
        public static DiagnosticDescriptor Rule => DiagnosticDescriptors.EPC18;

        /// <nodoc />
        public TaskInstanceToStringConversionAnalyzer()
            : base(Rule)
        {
        }

        /// <inheritdoc />
        protected override void InitializeCore(AnalysisContext context)
        {
            context.RegisterOperationAction(AnalyzeConversion, OperationKind.Conversion);
            context.RegisterOperationAction(AnalyzeInterpolation, OperationKind.Interpolation);
        }

        private void AnalyzeInterpolation(OperationAnalysisContext context)
        {
            // This method checks for $"foobar: {taskLikeThing}";
            if (context.Operation is IInterpolationOperation interpolationOperation)
            {
                if (interpolationOperation.Expression.Type.IsTaskLike(context.Compilation))
                {
                    var diagnostic = Diagnostic.Create(Rule, interpolationOperation.Expression.Syntax.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private void AnalyzeConversion(OperationAnalysisContext context)
        {
            // This method checks for "something" + taskLikeThing;
            // or string.Format("{0}", taskLikeThing);
            if (context.Operation is IConversionOperation conversion)
            {
                // A dangerous conversion is happening when the type of the type of the parent operation is string,
                // like in "FooBar: " + FooBarAsync()
                // Or the type of the grand parent operation is string,
                // like in string.Format("{0}", FooBarAsync())
                if (conversion.Operand.Type.IsTaskLike(context.Compilation) && 
                    (isString(conversion.Parent) || isString(conversion.Parent?.Parent)))
                {
                    var diagnostic = Diagnostic.Create(Rule, context.Operation.Syntax.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }
            }

            static bool isString(IOperation? operation) => operation?.Type?.SpecialType == SpecialType.System_String;
        }
    }
}