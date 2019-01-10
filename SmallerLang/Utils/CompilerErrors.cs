using SmallerLang.Lexer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmallerLang.Utils
{
    static class CompilerErrors
    {
        public static bool ErrorOccurred => _error.ErrorOccurred;

        static IErrorReporter _error;
        public static void SetReporter(IErrorReporter pError)
        {
            _error = pError;
        }

        #region Type Errors
        public static void UndeclaredType(string pType, TextSpan pSpan)
        {
            _error.WriteError($"Use of undeclared type {pType}", pSpan);
        }

        public static void TypeNotInScope(string pType, TextSpan pSpan)
        {
            _error.WriteError($"Type {pType} is inaccessible because it's marked private", pSpan);
        }

        public static void TypeCastError(string pFrom, string pTo, TextSpan pSpan)
        {
            _error.WriteError($"Cannot convert type {pFrom} to {pTo}", pSpan);
        }

        public static void TypeCastError(SmallType pFrom, SmallType pTo, TextSpan pSpan)
        {
            _error.WriteError($"Cannot convert type {pFrom.Name} to {pTo.Name}", pSpan);
        }

        public static void IteratorError(SmallType pType, TextSpan pSpan)
        {
            _error.WriteError($"Cannot iterate over type {pType} because it doesn't implement Enumerable", pSpan);
        }
        #endregion

        #region Type Definition Errors
        public static void DuplicateType(string pType, TextSpan pSpan)
        {
            _error.WriteError($"Type '{pType}' has already been defined", pSpan);
        }

        public static void DuplicateField(string pField, Syntax.TypeDefinitionSyntax pType, TextSpan pSpan)
        {
            _error.WriteError($"Duplicate field definition '{pField}' within struct {pType.Name}", pSpan);
        }

        public static void ImplementationMissingField(string pField, Syntax.TypeDefinitionSyntax pType, TextSpan pSpan)
        {
            _error.WriteError($"Implementation of trait {pType.Name} is missing field {pField}", pSpan);
        }

        public static void ImplementationMissingMethod(string pMethod, Syntax.TypeDefinitionSyntax pType, TextSpan pSpan)
        {
            _error.WriteError($"Implementation of trait {pType.Name} is missing method {pMethod}", pSpan);
        }

        public static void CircularReference(Syntax.TypeDefinitionSyntax pType, TextSpan pSpan)
        {
            _error.WriteError($"Found circular reference in struct {pType.Name}", pSpan);
        }

        public static void TypePolyArgumentCount(Syntax.TypeSyntax pType, int pCount, TextSpan pSpan)
        {
            _error.WriteError($"Using the generic type {SyntaxHelper.GetFullTypeName(pType)} requires {pCount} type arguments", pSpan);
        }

        public static void FieldNotInitialized(string pField, TextSpan pSpan)
        {
            _error.WriteError($"Field '{pField}' must be fully assigned before control is returned to the caller", pSpan);
        }
        #endregion

        #region Variable Errors
        public static void IdentifierAlreadyDeclared(Syntax.IdentifierSyntax pIdentifier, TextSpan pSpan)
        {
            _error.WriteError($"The name '{pIdentifier.Value}' is already declared in the current scope", pSpan);
        }

        public static void IdentifierNotDeclared(Syntax.IdentifierSyntax pIdentifier, TextSpan pSpan)
        {
            _error.WriteError($"The name '{pIdentifier.Value}' does not exist in the current context", pSpan);
        }

        public static void IdentifierNotDeclaredSelf(Syntax.IdentifierSyntax pIdentifier, TextSpan pSpan)
        {
            _error.WriteError($"The name '{pIdentifier.Value}' does not exist in the current context. Are you missing 'self'?", pSpan);
        }

        public static void IdentifierNotDeclared(SmallType pType, Syntax.IdentifierSyntax pIdentifier, TextSpan pSpan)
        {
            _error.WriteError($"Type {pType.Name} does not contain a definition for '{pIdentifier.Value}'", pSpan);
        }

        public static void AttemptDeclareTrait(SmallType pType, TextSpan pSpan)
        {
            _error.WriteError($"Type {pType.Name} is a trait and cannot be directly initialized", pSpan);
        }

        public static void ValueDefinedAsType(Syntax.IdentifierSyntax pIdentifier, TextSpan pSpan)
        {
            _error.WriteError($"Identifier {pIdentifier.Value} is already declared as a type", pSpan);
        }

        public static void DeclarationCountMismatch(int pCorrect, int pActual, TextSpan pSpan)
        {
            _error.WriteError($"Declaration specifies {pActual} values but {pCorrect} are expected", pSpan);
        }

        public static void AccessPrivateMember(Syntax.IdentifierSyntax pField, TextSpan pSpan)
        {
            _error.WriteError($"Cannot access member '{pField.Value}' because it is marked as private in the stuct", pSpan);
        }

        public static void CannotIndex(SmallType pType, TextSpan pSpan)
        {
            _error.WriteError($"Cannot apply indexing to an exception of type {pType}", pSpan);
        }
        #endregion

        #region External Methods Errors
        public static void ExternalAnnotationFormat(TextSpan pSpan)
        {
            _error.WriteError("External method annotations must be in format Assembly,Type,Method", pSpan);
        }

        public static void NoExternalAnnotation(TextSpan pSpan)
        {
            _error.WriteError("External methods must be annotated with where to locate the external method", pSpan);
        }

        public static void UnknownExternalMethod(string pMethod, TextSpan pSpan)
        {
            _error.WriteError($"Unable to find method {pMethod}", pSpan);
        }

        public static void StaticExternalMethod(string pMethod, TextSpan pSpan)
        {
            _error.WriteError($"Method {pMethod} must be a static method", pSpan);
        }

        public static void UnknownExternalError(string pError, TextSpan pSpan)
        {
            _error.WriteError($"Error validating external method: {pError}", pSpan);
        }
        #endregion

        #region Method Errors
        public static void MethodNoReturn(Syntax.MethodSyntax pMethod, TextSpan pSpan)
        {
            _error.WriteError($"Method {pMethod.Name} returns no values, a return keyword must not be followed by an expression", pSpan);
        }

        public static void MethodReturnPaths(Syntax.MethodSyntax pMethod, TextSpan pSpan)
        {
            _error.WriteError($"Not all code paths within {pMethod.Name} return a value", pSpan);
        }

        public static void MethodReturnCount(int pCount, TextSpan pSpan)
        {
            _error.WriteError($"Return must produce {pCount} expressions", pSpan);
        }

        public static void MethodNotFound(Emitting.MethodDefinition pDef, SmallType pType, string pMethod, IList<Syntax.SyntaxNode> pArguments, TextSpan pSpan)
        {
            if(pDef.Name != null)
            {
                //If we found a method but the types are wrong, print cast errors instead
                for (int i = 0; i < pDef.ArgumentTypes.Count; i++)
                {
                    if(!pArguments[i].Type.IsAssignableFrom(pDef.ArgumentTypes[i]))
                    {
                        TypeCastError(pArguments[i].Type, pDef.ArgumentTypes[i], pArguments[i].Span);
                    }
                }
            }
            else if(pType == null)
            {
                _error.WriteError($"Method '{pMethod}' not declared in the current context", pSpan);
            }
            else
            {
                _error.WriteError($"Type {pType} does not declare a method '{pMethod}'", pSpan);
            }
        }

        public static void MethodNotInScope(Emitting.MethodDefinition pDef, SmallType pType, string pMethod, IList<Syntax.SyntaxNode> pArguments, TextSpan pSpan)
        {
            _error.WriteError($"Method {pMethod} is inaccessible because of it's marked as private", pSpan);
        }

        public static void MethodDuplicate(Syntax.MethodSyntax pMethod, TextSpan pSpan)
        {
            _error.WriteError($"Method called '{pMethod.Name}' already defined with the same parameter types", pSpan);
        }

        public static void ConstructorMethodArguments(SmallType pType, int pExpected, int pActual, TextSpan pSpan)
        {
            _error.WriteError($"Constructor to {pType.Name} is expecting {pExpected} argument(s) but has {pActual}", pSpan);
        }
        #endregion

        #region Run Method Errors
        public static void NoRunMethod(TextSpan pSpan)
        {
            _error.WriteError("No run method found", pSpan);
        }

        public static void RunMethodParameters(TextSpan pSpan)
        {
            _error.WriteError("Run method must have no parameters", pSpan);
        }

        public static void RunMethodReturn(TextSpan pSpan)
        {
            _error.WriteError("Run method must not have no return value", pSpan);
        }

        public static void RunMethodDuplicate(string pMethod1, string pMethod2, TextSpan pSpan)
        {
            _error.WriteError($"Multiple @run methods found: {pMethod1}, {pMethod2}", pSpan);
        }
        #endregion

        #region Cast Errors
        public static void TooLargeNumber(Syntax.NumericLiteralSyntax pValue, string pType, TextSpan pSpan)
        {
            _error.WriteError($"Constant value {pValue.Value} is too large for {pType}", pSpan);
        }

        public static void InferImplicitCast(TextSpan pSpan)
        {
            _error.WriteError("Cannot infer type of implicit cast. Try using an explicit cast instead", pSpan);
        }

        public static void CastDuplicate(SmallType pType1, SmallType pType2, TextSpan pSpan)
        {
            _error.WriteError($"Cast already defined for type {pType1} to {pType2}", pSpan);
        }

        public static void CastDoesNotExist(SmallType pType1, SmallType pType2, TextSpan pSpan)
        {
            _error.WriteError($"Cast for types {pType1} to {pType2} has not been defined", pSpan);
        }

        public static void CastNotIsScope(SmallType pType1, SmallType pType2, TextSpan pSpan)
        {
            _error.WriteError($"Cast for types {pType1} to {pType2} is inaccessible because it's marked as private", pSpan);
        }
        #endregion

        #region Namespace Errors
        public static void NamespaceNotDefined(Syntax.NamespaceSyntax pNamespace, TextSpan pSpan)
        {
            NamespaceNotDefined(pNamespace.Value, pSpan);
        }

        public static void NamespaceNotDefined(string pNamespace, TextSpan pSpan)
        {
            _error.WriteError($"Namespace {pNamespace} has not been defined", pSpan);
        }

        public static void StructNamespace(TextSpan pSpan)
        {
            _error.WriteError("Struct types cannot specify namespaces", pSpan);
        }

        public static void DuplicateNamespaceAlias(string pAlias, TextSpan pSpan)
        {
            _error.WriteError($"Module with alias '{pAlias}' has already been imported", pSpan);
        }
        #endregion

        #region Other Errors
        public static void InvalidDefer(TextSpan pSpan)
        {
            _error.WriteError("Statement is unable to be deferred", pSpan);
        }

        public static void InvalidBreakCount(int pCount, TextSpan pSpan)
        {
            _error.WriteError($"Invalid break count, cannot be larger than {pCount}", pSpan);
        }

        public static void InvalidBreakLocation(TextSpan pSpan)
        {
            _error.WriteError("Break statements can only appear in loops or case statements", pSpan);
        }

        public static void SelectComplete(string pValues, TextSpan pSpan)
        {
            _error.WriteError($"Complete select must contain all values. Missing: {pValues}", pSpan);
        }

        public static void UnknownCharacter(char pChar, TextSpan pSpan)
        {
            _error.WriteError($"Encountered unknown character '{pChar}'", pSpan);
        }

        public static void UnableToReadFile(string pFile)
        {
            _error.WriteError($"Unable to read file '{pFile}'");
        }

        public static void FileNotFound(string pFile)
        {
            _error.WriteError($"File '{pFile}' not found");
        }

        public static void UnknownToken(TokenType pToken, TextSpan pSpan)
        {
            _error.WriteError($"Unknown token {pToken}", pSpan);
        }

        public static void GenericError(string pError, TextSpan pSpan)
        {
            _error.WriteError(pError, pSpan);
        }
        #endregion

        #region Warnings
        public static void UnreachableCode(TextSpan pSpan)
        {
            _error.WriteWarning("Unreachable code detected", pSpan);
        }

        public static void IgnoredComplete(TextSpan pSpan)
        {
            _error.WriteWarning("complete annotation is ignored on select statements with 'it'", pSpan);
        }

        public static void VariableNeverUsed(string pVariable)
        {
            _error.WriteWarning($"Variable {pVariable} is defined but never used");
        }

        public static void CompleteNonEnum(TextSpan pSpan)
        {
            _error.WriteWarning("complete annotation can only be used with enum types", pSpan);
        }
        #endregion
    }
}
