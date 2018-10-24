using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Emitting;

namespace SmallerLang.Validation
{
    partial class PostTypeValidationVisitor : SyntaxNodeVisitor
    {
        readonly IErrorReporter _error;
        private HashSet<string> _usedFields;

        public PostTypeValidationVisitor(IErrorReporter pError)
        {
            _error = pError;
        }

        protected override void VisitMethodSyntax(MethodSyntax pNode)
        {
            //Validate that one and only 1 method is annotated with "run"
            //This method must contain no parameters and return no values
            if (pNode.Annotation.Value == Utils.KeyAnnotations.RunMethod)
            {
                if (Store.GetValue<string>("RunMethod") != null)
                {
                    _error.WriteError($"Two run methods found: {Store.GetValue<string>("RunMethod")} and {pNode.Name}", pNode.Span);
                    return;
                }

                Store.SetValue("RunMethod", pNode.Name);
                if (pNode.Parameters.Count != 0)
                {
                    _error.WriteError("Run method must have no parameters", pNode.Span);
                }

                if (pNode.ReturnValues.Count != 0)
                {
                    _error.WriteError("Run method must not return a value", pNode.Span);
                }
            }

            using (var ic = Store.AddValue("InConstructor", pNode.Annotation.Value == Utils.KeyAnnotations.Constructor))
            {
                using (var rf = Store.AddValue("ReturnFound", false))
                {
                    using (var rvc = Store.AddValue("ReturnValueCount", pNode.ReturnValues.Count))
                    {
                        _usedFields = new HashSet<string>();
                        base.VisitMethodSyntax(pNode);

                        //Validate that all paths return a value
                        if (pNode.Body != null)
                        {
                            if (pNode.ReturnValues.Count != 0 && !rf.Value)
                            {
                                _error.WriteError("Not all code paths return a value", pNode.Span);
                            }
                            else if (pNode.ReturnValues.Count == 0 && rf.Value)
                            {
                                _error.WriteError("Method has no return value, so no return statement must be present", pNode.Span);
                            }
                        }
                    }
                }

                if (ic.Value)
                {
                    SmallType s = Struct;
                    if(s != null)
                    {
                        foreach (var f in s.GetFields())
                        {
                            if (!_usedFields.Contains(f.Name))
                            {
                                _error.WriteError($"Field {f.Name} is not initialized");
                            }
                        }
                    }
                }
            }
        }

        protected override void VisitAssignmentSyntax(AssignmentSyntax pNode)
        {
            using (var ia = Store.AddValue("InAssignment", true))
            {
                base.VisitAssignmentSyntax(pNode);
            }
        }

        protected override void VisitCastSyntax(CastSyntax pNode)
        {
            base.VisitCastSyntax(pNode);

            if(!pNode.FromType.IsAssignableFrom(pNode.Type) &&
               (!IsStandard(pNode.FromType) || !IsStandard(pNode.Type)))
            {
                if (MethodCache.CastExists(pNode.FromType, pNode.Type, out MethodDefinition d))
                {
                    pNode.SetMethod(d);
                }
                else
                {
                    _error.WriteError($"No cast defined for types {pNode.FromType.ToString()} and {pNode.Type.ToString()}");
                }
            }
        }

        protected override void VisitCastDefinitionSyntax(CastDefinitionSyntax pNode)
        {
            using (var rf = Store.AddValue("ReturnFound", false))
            {
                using (var rvc = Store.AddValue("ReturnValueCount", pNode.ReturnValues.Count))
                {
                    base.VisitCastDefinitionSyntax(pNode);

                    if (pNode.ReturnValues.Count != 0 && !rf.Value)
                    {
                        _error.WriteError("Not all code paths return a value", pNode.Span);
                    }
                    else if (pNode.ReturnValues.Count == 0 && rf.Value)
                    {
                        _error.WriteError("Method has no return value, so no return statement must be present", pNode.Span);
                    }
                }
            }
        }

        protected override void VisitIfSyntax(IfSyntax pNode)
        {
            Visit((dynamic)pNode.Condition);
            Visit(pNode.Body);

            if(pNode.Else != null)
            {
                var found = Store.GetValue<bool>("ReturnFound");
                Store.SetValue("ReturnFound", false);

                Visit(pNode.Else);

                //Returns must be found in ALL else blocks
                Store.SetValue("ReturnFound", found && Store.GetValue<bool>("ReturnFound"));
            }
        }

        protected override void VisitReturnSyntax(ReturnSyntax pNode)
        {
            Store.SetValue("ReturnFound", true);
            var count = Store.GetValue<int>("ReturnValueCount");
            if(pNode.Values.Count != count)
            {
                _error.WriteError($"Method must return {count} values", pNode.Span);
            }

            if (pNode.Deferred)
            {
                _error.WriteError("Unable to defer a return statement", pNode.Span);
            }
            base.VisitReturnSyntax(pNode);
        }

        private static bool IsStandard(SmallType pType)
        {
            return pType == SmallTypeCache.Short ||
                   pType == SmallTypeCache.Int ||
                   pType == SmallTypeCache.Long ||
                   pType == SmallTypeCache.Float ||
                   pType == SmallTypeCache.Double ||
                   pType == SmallTypeCache.String ||
                   pType == SmallTypeCache.Boolean;
        }
    }
}
