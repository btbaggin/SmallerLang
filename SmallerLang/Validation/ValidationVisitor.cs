using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Emitting;

namespace SmallerLang.Validation
{
    partial class PostTypeValidation : SyntaxNodeVisitor
    {
        string _runMethod;
        VariableCache<(bool Used, TextSpan Span)> _locals; //Used to find unused variables

        protected override void VisitDeclarationSyntax(DeclarationSyntax pNode)
        {
            foreach(var v in pNode.Variables)
            {
                Visit((dynamic)v);
                _locals.DefineVariableInScope(v.Value, (false, v.Span));
            }
            Visit((dynamic)pNode.Value);
        }

        protected override void VisitIdentifierSyntax(IdentifierSyntax pNode)
        {
            _locals.SetVariableValue(pNode.Value, (true, default));
            base.VisitIdentifierSyntax(pNode);
        }

        protected override void VisitModuleSyntax(ModuleSyntax pNode)
        {
            _locals = new VariableCache<(bool, TextSpan)>();
            base.VisitModuleSyntax(pNode);
            if(_runMethod == null)
            {
                _error.WriteError("No run method found!", pNode.Span);
            }
        }

        private void ValidateRun(MethodSyntax pNode)
        {
            //Validate that one and only 1 method is annotated with "run"
            //This method must contain no parameters and return no values
            if (pNode.Annotation == Utils.KeyAnnotations.RunMethod)
            {
                if (_runMethod != null)
                {
                    _error.WriteError($"Two run methods found: {_runMethod} and {pNode.Name}", pNode.Span);
                    return;
                }

                _runMethod = pNode.Name;
                if (pNode.Parameters.Count != 0)
                {
                    _error.WriteError("Run method must have no parameters", pNode.Span);
                }

                if (pNode.ReturnValues.Count != 0)
                {
                    _error.WriteError("Run method must not return a value", pNode.Span);
                }
            }
        }

        protected override void VisitBlockSyntax(BlockSyntax pNode)
        {
            //Validate that deferred statements do not contain "return" statements
            //Report any unused variables
            if(pNode.Deferred && Utils.SyntaxHelper.LastStatementIsReturn(pNode))
            {
                _error.WriteError("Unable to defer a return statement", pNode.Span);
            }

            //Report any unused variables
            _locals.AddScope();
            base.VisitBlockSyntax(pNode);
            foreach (var (Variable, Value) in _locals.GetVariablesInScope())
            {
                if (!Value.Used)
                {
                    _error.WriteWarning($"Variable {Variable} is defined but never used", Value.Span);
                }
            }
            _locals.RemoveScope();
        }

        protected override void VisitSelectSyntax(SelectSyntax pNode)
        {
            //Validate if a select is marked as complete, ensure all enum values are used
            if(Utils.SyntaxHelper.SelectIsComplete(pNode))
            {
                var t = pNode.Condition.Type;
                if(!t.IsEnum)
                {
                    _error.WriteWarning("complete annotation can only be used with enum types", pNode.Condition.Span);
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
                        //Default marks all as found
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
                        _error.WriteError("select marked as complete does not contain values: " + fields.Aggregate(new StringBuilder(), (c, f) =>
                        {
                            if (c.Length > 0) c.Append(", ");
                            c.Append(f);
                            return c;
                        }), pNode.Span);

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
                    if(!double.TryParse(pNode.Value, out double d)) _error.WriteError("Value is too large for a double", pNode.Span);
                    break;

                case NumberTypes.Float:
                    if (!float.TryParse(pNode.Value, out float f)) _error.WriteError("Value is too large for a float", pNode.Span);
                    break;

                case NumberTypes.Integer:
                    if (!int.TryParse(pNode.Value, out int i)) _error.WriteError("Value is too large for a int", pNode.Span);
                    break;

                case NumberTypes.Long:
                    if (!long.TryParse(pNode.Value, out long l)) _error.WriteError("Value is too large for a long", pNode.Span);
                    break;

                case NumberTypes.Short:
                    if (!short.TryParse(pNode.Value, out short s)) _error.WriteError("Value is too large for a short", pNode.Span);
                    break;
            }
            base.VisitNumericLiteralSyntax(pNode);
        }
    }
}
