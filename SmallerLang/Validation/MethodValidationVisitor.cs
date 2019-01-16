using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Emitting;
using SmallerLang.Utils;

namespace SmallerLang.Validation
{
    partial class PostTypeValidationVisitor : SyntaxNodeVisitor
    {
        private HashSet<string> _usedFields;

        protected override void VisitMethodSyntax(MethodSyntax pNode)
        {
            if (pNode.External)
            {
                KeyAnnotations.ValidateExternalAnnotation(pNode.Annotation, pNode);
            }

            //Validate that one and only 1 method is annotated with "run"
            //This method must contain no parameters and return no values
            if (pNode.Annotation.Value == KeyAnnotations.RunMethod)
            {
                if (Store.GetValue<string>("RunMethod") != null)
                {
                    CompilerErrors.RunMethodDuplicate(Store.GetValue<string>("RunMethod"), pNode.Name, pNode.Span);
                    return;
                }

                Store.SetValue("RunMethod", pNode.Name);
                if (pNode.Parameters.Count != 0)
                {
                    CompilerErrors.RunMethodParameters(pNode.Span);
                }

                if (pNode.ReturnValues.Count != 0)
                {
                    CompilerErrors.RunMethodReturn(pNode.Span);
                }
            }

            using (var ic = Store.AddValue("InConstructor", pNode.Annotation.Value == KeyAnnotations.Constructor))
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
                                CompilerErrors.MethodReturnPaths(pNode, pNode.Span);
                            }
                            else if (pNode.ReturnValues.Count == 0 && rf.Value)
                            {
                                CompilerErrors.MethodNoReturn(pNode, pNode.Span);
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
                                CompilerErrors.FieldNotInitialized(f.Name, pNode.Span);
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
                switch (_unit.CastExists(pNode.FromType, pNode.Type, out MethodDefinition d))
                {
                    case Compiler.FindResult.Found:
                        pNode.SetMethod(d);
                        break;

                    case Compiler.FindResult.NotFound:
                        CompilerErrors.CastDoesNotExist(pNode.FromType, pNode.Type, pNode.Span);
                        break;

                    case Compiler.FindResult.IncorrectScope:
                        CompilerErrors.CastNotIsScope(pNode.FromType, pNode.Type, pNode.Span);
                        break;
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
                        CompilerErrors.MethodReturnPaths(pNode, pNode.Span);
                    }
                    else if (pNode.ReturnValues.Count == 0 && rf.Value)
                    {
                        CompilerErrors.MethodNoReturn(pNode, pNode.Span);
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
            Store.SetValue("ReturnFound", pNode.Values.Count > 0);
            var count = Store.GetValue<int>("ReturnValueCount");

            //If we have 0 return values it will be caught when we return to the method for checking return statements
            if(count > 0 && pNode.Values.Count != count)
            {
                CompilerErrors.MethodReturnCount(count, pNode.Span);
            }

            //Return statements can't be deferred otherwise we would defer forever!
            if (pNode.Deferred)
            {
                CompilerErrors.InvalidDefer(pNode.Span);
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
                   pType == SmallTypeCache.Boolean ||
                   pType == SmallTypeCache.Char;
        }
    }
}
