using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Emitting;
using SmallerLang.Utils;

namespace SmallerLang.Operations.Validation
{
    struct LocalReference
    {
        public bool IsReferenced { get; set; }
        public TextSpan Span { get; private set; }
        public string Name { get; private set; }

        public LocalReference(TextSpan pSpan, string pName, bool pIsReferenced)
        {
            IsReferenced = pIsReferenced;
            Span = pSpan;
            Name = pName;
        }
    }

    partial class PostTypeValidationVisitor : SyntaxNodeVisitor
    {
        ScopeCache<LocalReference> _locals; //Used to find unused variables
        ModuleSyntax _mainModule;
        Compiler.CompilationCache _unit;

        public PostTypeValidationVisitor(Compiler.CompilationCache pUnit)
        {
            _unit = pUnit;
        }

        protected override void VisitDeclarationSyntax(DeclarationSyntax pNode)
        {
            if(!pNode.IsConst)
            {
                foreach (var v in pNode.Variables)
                {
                    Visit(v);
                    _locals.DefineVariableInScope(v.Value, new LocalReference(v.Span, v.Value, false));
                }
                Visit(pNode.Value);
            }
        }

        protected override void VisitIdentifierSyntax(IdentifierSyntax pNode)
        {
            _locals.SetValue(pNode.Value, new LocalReference(pNode.Span, pNode.Value, true));
            var currentStruct = Struct;
            var currentType = CurrentType;

            if(currentType != null)
            {
                var definition = currentType.GetField(pNode.Value);

                //Only the defining struct can access hidden fields
                if (definition.Visibility == FieldVisibility.Hidden && currentStruct != currentType)
                {
                    //Check if the struct is a trait that implements the current type
                    //This will allow implementing traits to access the struct's private fields
                    if (currentStruct == null || !currentStruct.IsTrait || !currentType.IsAssignableFrom(currentStruct))
                    {
                        CompilerErrors.AccessPrivateMember(pNode, pNode.Span);
                    }
                }
            }

            if(currentStruct != null && 
               Store.GetValueOrDefault<bool>("InConstructor") && 
               Store.GetValueOrDefault<bool>("InAssignment"))
            {
                _usedFields.Add(pNode.Value);
            }
            base.VisitIdentifierSyntax(pNode);
        }

        protected override void VisitWorkspaceSyntax(WorkspaceSyntax pNode)
        {
            _mainModule = pNode.Module;
            base.VisitWorkspaceSyntax(pNode);
        }

        protected override void VisitModuleSyntax(ModuleSyntax pNode)
        {
            _locals = new ScopeCache<LocalReference>();
            _locals.AddScope();
            using (var v = Store.AddValue<string>("RunMethod", null))
            {
                base.VisitModuleSyntax(pNode);

                if (pNode == _mainModule && v.Value == null)
                {
                    CompilerErrors.NoRunMethod(pNode.Span);
                }
            }
            _locals.RemoveScope();
        }

        protected override void VisitBlockSyntax(BlockSyntax pNode)
        {
            //Validate that deferred statements do not contain "return" statements
            //Report any unused variables
            if(pNode.Deferred && SyntaxHelper.LastStatementIsReturn(pNode))
            {
                CompilerErrors.InvalidDefer(pNode.Span);
            }

            //Report any unused variables
            _locals.AddScope();
            base.VisitBlockSyntax(pNode);
            foreach (var ld in _locals.GetVariablesInScope())
            {
                if (!ld.IsReferenced)
                {
                    CompilerErrors.VariableNeverUsed(ld.Name, ld.Span);
                }
            }
            _locals.RemoveScope();
        }

        protected override void VisitSelectSyntax(SelectSyntax pNode)
        {
            //Validate if a select is marked as complete, ensure all enum values are used
            if(pNode.Annotation.Value == KeyAnnotations.Complete)
            {
                var t = pNode.Condition.Type;
                if(!t.IsEnum)
                {
                    CompilerErrors.CompleteNonEnum(pNode.Condition.Span);
                }
                else
                {
                    //Get all enum values
                    //We will indicate a field has been found by making the string null
                    List<string> fields = new List<string>();
                    foreach (var f in t.GetFields())
                    {
                        fields.Add(f.Name);
                    }

                    foreach (var c in pNode.Cases)
                    {
                        //Default covers all possible cases so mark all as found
                        if (c.IsDefault)
                        {
                            for (int i = 0; i < fields.Count; i++)
                            {
                                fields[i] = null;
                            }
                        }
                        else
                        {
                            foreach (var cd in c.Conditions)
                            {
                                //We can only check numeric literals and enum access
                                //We cannot validate returning from a method
                                if (cd is NumericLiteralSyntax n)
                                {
                                    for (int i = 0; i < fields.Count; i++)
                                    {
                                        if (t.GetEnumValue(fields[i]) == int.Parse(n.Value)) fields[i] = null;
                                    }
                                }
                                else if (cd is MemberAccessSyntax m)
                                {
                                    for (int i = 0; i < fields.Count; i++)
                                    {
                                        if (fields[i] == m.Value.Value) fields[i] = null;
                                    }
                                }
                            }
                        }
                    }

                    fields = fields.Where((f) => f != null).ToList();
                    if(fields.Count > 0)
                    {
                        CompilerErrors.SelectComplete(fields.Aggregate(new StringBuilder(), (c, f) =>
                        {
                            if (c.Length > 0) c.Append(", ");
                            c.Append(f);
                            return c;
                        }).ToString(), pNode.Span);
                    }
                }
            }
            base.VisitSelectSyntax(pNode);
        }

        protected override void VisitNumericLiteralSyntax(NumericLiteralSyntax pNode)
        {
            //Validate that numbers are appropriately sized to be stored in their proper type
            switch(pNode.NumberType)
            {
                case NumberTypes.Double:
                    if(!double.TryParse(pNode.Value, out double d)) CompilerErrors.TooLargeNumber(pNode, "Double", pNode.Span);
                    break;

                case NumberTypes.Float:
                    if (!float.TryParse(pNode.Value, out float f)) CompilerErrors.TooLargeNumber(pNode, "Float", pNode.Span);
                    break;

                case NumberTypes.Integer:
                    if (!int.TryParse(pNode.Value, out int i)) CompilerErrors.TooLargeNumber(pNode, "Int", pNode.Span);
                    break;

                case NumberTypes.Long:
                    if (!long.TryParse(pNode.Value, out long l)) CompilerErrors.TooLargeNumber(pNode, "Long", pNode.Span);
                    break;

                case NumberTypes.Short:
                    if (!short.TryParse(pNode.Value, out short s)) CompilerErrors.TooLargeNumber(pNode, "Short", pNode.Span);
                    break;
            }
            base.VisitNumericLiteralSyntax(pNode);
        }

        int _breakCount;
        protected override void VisitCaseSyntax(CaseSyntax pNode)
        {
            _locals.AddScope();
            foreach (var c in pNode.Conditions)
            {
                Visit(c);
            }

            using (var iw = Store.AddValue("CanBreak", true))
            {
                _breakCount++;
                Visit(pNode.Body);
                _breakCount--;
            }
            _locals.RemoveScope();
        }

        protected override void VisitForSyntax(ForSyntax pNode)
        {
            _locals.AddScope();
            if (pNode.Iterator != null)
            {
                Visit(pNode.Iterator);
            }
            else
            {
                foreach (var d in pNode.Initializer)
                {
                    Visit(d);
                }
                Visit(pNode.Condition);

                foreach (var f in pNode.Finalizer)
                {
                    Visit(f);
                }
            }

            using (var iw = Store.AddValue("CanBreak", true))
            {
                _breakCount++;
                Visit(pNode.Body);
                _breakCount--;
            }
            _locals.RemoveScope();
        }

        protected override void VisitWhileSyntax(WhileSyntax pNode)
        {
            _locals.AddScope();
            Visit(pNode.Condition);
            using (var iw = Store.AddValue("CanBreak", true))
            {
                _breakCount++;
                Visit(pNode.Body);
                _breakCount--;
            }
            _locals.RemoveScope();
        }

        protected override void VisitBreakSyntax(BreakSyntax pNode)
        {
            if(!Store.GetValueOrDefault<bool>("CanBreak"))
            {
                CompilerErrors.InvalidBreakLocation(pNode.Span);
            }
            else
            {
                if(pNode.CountAsInt >= _breakCount)
                {
                    CompilerErrors.InvalidBreakCount(_breakCount - 1, pNode.Span);
                }
            }
        }
    }
}
