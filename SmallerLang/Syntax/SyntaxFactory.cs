using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmallerLang.Syntax
{
    public static class SyntaxFactory
    {
        public static ModuleSyntax Module(IDictionary<string, ModuleSyntax> pImports, IList<MethodSyntax> pMethods, IList<TypeDefinitionSyntax> pDefinitions, IList<EnumSyntax> pEnums)
        {
            return new ModuleSyntax(pImports, pMethods, pDefinitions, pEnums);
        }

        public static StringLiteralSyntax StringLiteral(string pValue)
        {
            return new StringLiteralSyntax(pValue);
        }

        public static BooleanLiteralSyntax BooleanLiteral(string pValue)
        {
            return new BooleanLiteralSyntax(pValue);
        }

        public static NumericLiteralSyntax NumericLiteral(string pValue, NumberTypes pType)
        {
            return new NumericLiteralSyntax(pValue, pType);
        }

        public static NumericLiteralSyntax NumericLiteral(int pValue)
        {
            return new NumericLiteralSyntax(pValue.ToString(), NumberTypes.Integer);
        }

        public static ArrayLiteralSyntax ArrayLiteral(TypeSyntax pType, SyntaxNode pSize)
        {
            return new ArrayLiteralSyntax(pType, pSize);
        }

        public static ArrayAccessSyntax ArrayAccess(IdentifierSyntax pIdentifier, SyntaxNode pValue)
        {
            return new ArrayAccessSyntax(pIdentifier, pValue);
        }

        public static BlockSyntax Block(IList<SyntaxNode> pStatements)
        {
            return new BlockSyntax(pStatements);
        }

        public static BlockSyntax SingleBlock(SyntaxNode pStatement)
        {
            List<SyntaxNode> statements = new List<SyntaxNode>(1);
            if (pStatement != null) statements.Add(pStatement);
            return Block(statements);
        }

        public static IfSyntax If(SyntaxNode pCondition, BlockSyntax pBody, ElseSyntax pElse)
        {
            return new IfSyntax(pCondition, pBody, pElse);
        }

        public static ElseSyntax Else(IfSyntax pIf, BlockSyntax pBody)
        {
            return new ElseSyntax(pBody, pIf);
        }

        public static WhileSyntax While(SyntaxNode pCondition, BlockSyntax pBody)
        {
            return new WhileSyntax(pCondition, pBody);
        }

        public static ForSyntax For(SyntaxNode pIterator, bool pBackwards, BlockSyntax pBody)
        {
            return new ForSyntax(pIterator, pBackwards, pBody);
        }

        public static ForSyntax For(IList<DeclarationSyntax> pInitializer, SyntaxNode pCondition, IList<SyntaxNode> pFinalizer, BlockSyntax pBody)
        {
            return new ForSyntax(pInitializer, pCondition, pFinalizer, pBody);
        }

        public static MethodSyntax ExternalMethod(FileScope pScope, string pName, TypeSyntax pReturns, IList<TypedIdentifierSyntax> pParameters, BlockSyntax pBody)
        {
            List<TypeSyntax> returns = new List<TypeSyntax>();
            if (pReturns != null) returns.Add(pReturns);

            return new MethodSyntax(pScope, pName, returns, pParameters, pBody, true);
        }

        public static MethodSyntax Method(FileScope pScope, string pName, IList<TypeSyntax> pReturns, IList<TypedIdentifierSyntax> pParameters, BlockSyntax pBody)
        {
            return new MethodSyntax(pScope, pName, pReturns, pParameters, pBody, false);
        }

        public static MethodSyntax Method(FileScope pScope, string pName, IList<TypeSyntax> pReturns, IList<TypedIdentifierSyntax> pParameters, BlockSyntax pBody, bool pExternal)
        {
            return new MethodSyntax(pScope, pName, pReturns, pParameters, pBody, pExternal);
        }

        public static TypeDefinitionSyntax TypeDefinition(FileScope pScope, TypeSyntax pType, TypeSyntax pImplements, DefinitionTypes pDefinitionType, IList<MethodSyntax> pMethods, IList<TypedIdentifierSyntax> pFields)
        {
            return new TypeDefinitionSyntax(pScope, pType, pImplements, pDefinitionType, pFields, pMethods);
        }

        public static StructInitializerSyntax StructInitializer(IList<IdentifierSyntax> pValues, TypeSyntax pStruct, IList<SyntaxNode> pArguments)
        {
            return new StructInitializerSyntax(pValues, pStruct, pArguments);
        }

        public static MethodCallSyntax MethodCall(string pName, IList<SyntaxNode> pArguments)
        {
            return new MethodCallSyntax(pName, pArguments);
        }

        public static BinaryExpressionSyntax BinaryExpression(SyntaxNode pLeft, BinaryExpressionOperator pOperator, SyntaxNode pRight)
        {
            return new BinaryExpressionSyntax(pLeft, pOperator, pRight);
        }

        public static UnaryExpressionSyntax UnaryExpression(SyntaxNode pValue, UnaryExpressionOperator pOperator)
        {
            return new UnaryExpressionSyntax(pValue, pOperator);
        }

        public static CastSyntax Cast(SyntaxNode pValue)
        {
            return new CastSyntax(pValue);
        }

        public static CastSyntax Cast(SyntaxNode pValue, TypeSyntax pType)
        {
            return new CastSyntax(pValue, pType);
        }

        public static CastDefinitionSyntax CastDefinition(FileScope pScope, TypedIdentifierSyntax pFromType, BlockSyntax pBody, TypeSyntax pToType)
        {
            return new CastDefinitionSyntax(pScope, pFromType, pBody, pToType);
        }

        public static DeclarationSyntax Declaration(IList<IdentifierSyntax> pVariables, SyntaxNode pRight)
        {
            return new DeclarationSyntax(pVariables, pRight);
        }

        public static AssignmentSyntax Assignment(IList<IdentifierSyntax> pVariables, AssignmentOperator pOp, SyntaxNode pValue)
        {
            return new AssignmentSyntax(pVariables, pOp, pValue);
        }

        public static IdentifierSyntax Identifier(string pValue)
        {
            return new IdentifierSyntax(pValue);
        }

        public static TypedIdentifierSyntax TypedIdentifier(TypeSyntax pType, string pValue)
        {
            return new TypedIdentifierSyntax(pType, pValue);
        }

        public static GenericTypeSyntax GenericType(SmallType pType)
        {
            return new GenericTypeSyntax(pType);
        }

        public static TypeSyntax Type(string pValue)
        {
            return Type(null, pValue, new List<TypeSyntax>());
        }

        public static TypeSyntax Type(string pValue, IList<TypeSyntax> pGenericArgs)
        {
            return Type(null, pValue, pGenericArgs);
        }

        public static TypeSyntax Type(NamespaceSyntax pNamespace, string pValue, IList<TypeSyntax> pGenericArgs)
        {
            return new TypeSyntax(pNamespace, pValue, pGenericArgs);
        }

        public static ReturnSyntax Return(IList<SyntaxNode> pValue)
        {
            return new ReturnSyntax(pValue);
        }

        public static MemberAccessSyntax MemberAccess(IdentifierSyntax pIdentifier, IdentifierSyntax pValue)
        {
            return new MemberAccessSyntax(pIdentifier, pValue);
        }

        public static SelectSyntax Select(SyntaxNode pCondition, IList<CaseSyntax> pCases)
        {
            return new SelectSyntax(pCondition, pCases);
        }

        public static CaseSyntax Case(IList<SyntaxNode> pConditions, BlockSyntax pBody)
        {
            return new CaseSyntax(pConditions, pBody);
        }

        public static ItSyntax It()
        {
            return new ItSyntax();
        }

        public static SelfSyntax Self()
        {
            return new SelfSyntax();
        }

        public static DiscardSyntax Discard()
        {
            return new DiscardSyntax();
        }

        public static EnumSyntax Enum(FileScope pScope, string pName, IList<IdentifierSyntax> pNames, IList<int> pValues)
        {
            return new EnumSyntax(pScope, pName, pNames, pValues);
        }

        public static BreakSyntax Break(string pCount)
        {
            return new BreakSyntax(pCount);
        }

        public static WorkspaceSyntax Workspace(string pName, ModuleSyntax pModule)
        {
            return new WorkspaceSyntax(pName, pModule);
        }

        public static NamespaceSyntax Namespace(string pAlias)
        {
            return new NamespaceSyntax(pAlias);
        }
    }
}
