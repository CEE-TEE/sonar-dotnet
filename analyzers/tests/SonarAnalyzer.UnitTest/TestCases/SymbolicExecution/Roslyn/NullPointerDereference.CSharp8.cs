﻿using System;

namespace Tests.Diagnostics.CSharp8
{
    public class NullCoalescenceAssignment
    {
        public void NullCoalescenceOperator_Null()
        {
            string name = null;
            name = name ?? "Laxmi";
            name.ToString();
        }

        public void NullCoalescenceOperator_Null_Noncompliant1()
        {
            string name = null;
            name = name ?? null;
            name.ToString(); // FN Suppressed #6117 {{'name' is null on at least one execution path.}}
        }

        public void NullCoalescenceOperator_Null_Noncompliant2()
        {
            string name = null;
            name = name ?? name.ToString(); // FN Suppressed #6117
        }

        public void NullCoalesce_Conversion_DownCast(AggregateException arg)
        {
            var value = arg as Exception ?? new Exception(arg.Message);     // FN Suppressed #6117, arg must be null on the right side
        }

        public void NullCoalesce_Conversion_UpCast(Exception arg)
        {
            var value = arg as AggregateException ?? new AggregateException(arg.Message);     // FN Suppressed #6117, arg can be null or another Exception type on the right side
        }

        public void NullCoalescenceAssignment_Null()
        {
            string name = null;
            name ??= name.ToString(); // Noncompliant
        }

        public void NullCoalescenceAssignment_NotNull_Noncompliant()
        {
            string name1 = null;
            string name2 = null;
            name1 ??= name2.ToString(); // Noncompliant {{'name2' is null on at least one execution path.}}
        }

        public void NullCoalescenceAssignment_NotNull()
        {
            string notNullString = "";
            string nullString = null;
            notNullString ??= nullString.ToString(); // Compliant, never visited

            object notNullObject = new object();
            object nullObject = null;
            notNullObject ??= nullObject.ToString(); // Compliant, never visited
        }

        public object SupportConversionFromDefault {
            get {
                object o = null;
                o.ToString();   // Noncompliant, just to make sure it is analyzed
                return default; // This should not fail the engine
            }
            set { }
        }

        public object ArrowProperty => default;
    }

    public class Nullables
    {
        public void NullablePrimitiveType()
        {
            int? i1 = null;
            i1 = i1 ?? i1.GetHashCode(); // FN, was supported before
        }
    }

    public class SwitchExpressions
    {
        public void Nullable_In_Arm_Noncompliant(string s)
        {
            var result = s switch
            {
                null => s.ToString(), // Noncompliant
                _ => s.ToString()
            };
        }

        public void AlwaysNull_Noncompliant(int val)
        {
            string result = val switch
            {
                1 => null,
                2 => null,
                _ => null
            };
            result.ToString(); // Noncompliant
        }

        public void MaybeNull_Noncompliant(int val)
        {
            string result = val switch
            {
                1 => "1",
                2 => "Not 1",
                _ => null
            };
            result.ToString(); // Noncompliant
        }

        public void AlwaysNonNull(int val)
        {
            string result = val switch
            {
                1 => "1",
                2 => "2",
                _ => "Neither 1 or 2"
            };
            result.ToString();
        }

        public void Nullable_In_Arm(string s)
        {
            var result = s switch
            {
                var x when x != null => s.ToString(),
                _ => s.ToString() // FN Switch expressions are not constrained (See #2949)
            };
        }

        public string NullCheck(object arg) =>
            arg switch
            {
                null => throw new ArgumentNullException(nameof(arg)),
                { } when IsInstance(arg) => "42",
                _ => throw new InvalidOperationException("Unexpected type: " + arg.GetType().Name)  // Compliant, arg cannot be null here
            };

        private static bool IsInstance(object arg) => false;

    }

    public class DefaultLiteral
    {
        void NoncompliantDefaultLiteral()
        {
            object obj = default;
            obj.ToString(); // Noncompliant
        }

        void CompliantDefaultLiteral()
        {
            int i = default;
            i.ToString();
        }
    }

    public class LocalStaticFunctions
    {
        public void Method()
        {
            string LocalFunction(string obj) =>
                obj != null ? null : obj.ToLower(); //  Compliant - FN: local functions are not supported by the CFG

            static string LocalStaticFunction(string obj) =>
                obj != null ? null : obj.ToLower(); //  Compliant - FN: local functions are not supported by the CFG
        }
    }

    public class ThrowHelper
    {
        public void DoesNotReturnIsRespectedOutsideNullableContext()
        {
            object o = null;
            DoesNotReturn();
            o.ToString(); // Compliant. Unreachable
        }

        public void TerminatesProgramIsRespected()
        {
            object o = null;
            TerminatesProgram();
            o.ToString(); // Compliant. Unreachable
        }

        [DoesNotReturn] // Custom attribute
        public void DoesNotReturn() { }

        [JetBrains.Annotations.TerminatesProgram]
        public void TerminatesProgram() { }
    }

    public sealed class DoesNotReturnAttribute : Attribute { }
}

namespace JetBrains.Annotations
{
    public sealed class TerminatesProgramAttribute : Attribute { }
}
