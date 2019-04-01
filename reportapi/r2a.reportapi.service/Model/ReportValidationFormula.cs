using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NLog;
using Pinecone.ReportFormula.Grammar;
using Pinecone.ReportFormula.Grammar.Nodes;
using Pinecone.ReportFormula.Interpreter;
using Tester.Model;
using ValueType = Pinecone.ReportFormula.Interpreter.ValueType;

namespace R2A.ReportApi.Service.Model
{
    public class ReportValidationFormula
    {
        public int Id { get; }
        public string Code { get; }
        public string Description { get; }
        public string AdditionalDescription { get; set; }
        public ValidationRuleSeverity Severity { get; }
        public string LeftFormula { get; }
        public string RightFormula { get; }
        public string ConditionFormula { get; }
        public string OperatorString { get; }
        public FormulaOperator Operator { get; }
        public decimal? Tolerance { get; }
        public bool Active { get; }
        public IAstNode LeftFormulaNode { get; private set; }
        public IAstNode RightFormulaNode { get; private set; }
        public IAstNode ConditionFormulaNode { get; private set; }
        public string FormulaString { get; }
        private readonly string _formulaResultString;
        private readonly string _formulaSourceString;
        public List<string> RequiredTemplatesLeft { get; }
        public List<string> RequiredTemplatesRight { get; }
        private bool _isProcessed = false;
        public string UserFriendlyFormula { get; set; }


        public bool IsValid =>
            LeftFormulaNode != null && RightFormulaNode != null && Operator != FormulaOperator.Unknown;

        public ReportValidationFormula(int id, string code, string description, string additionalDescription,
            int severity, string leftFormula, string rightFormula, string operatorString, decimal? tolerance,
            string conditionFormula, string requiredTemplatesLeft, string requiredTemplatesRight,
            string userFriendlyFormula, bool active, IFormatProvider formatProvider = null)
        {
            if (formatProvider == null)
                formatProvider = CultureInfo.CurrentCulture;
            Id = id;
            Code = code;
            Description = description;
            AdditionalDescription = additionalDescription;
            Severity = severity == (int) ValidationRuleSeverity.Warning
                ? ValidationRuleSeverity.Warning
                : ValidationRuleSeverity.Error;
            LeftFormula = leftFormula;
            RightFormula = rightFormula;
            ConditionFormula = conditionFormula;
            OperatorString = operatorString;
            Tolerance = tolerance;
            Active = active;
            switch (operatorString)
            {
                case "=":
                    Operator = FormulaOperator.Eq;
                    break;
                case "<>":
                case "!=":
                    Operator = FormulaOperator.Neq;
                    OperatorString = "<>";
                    break;
                case ">":
                    Operator = FormulaOperator.Gt;
                    break;
                case ">=":
                case "=>":
                    Operator = FormulaOperator.Gte;
                    OperatorString = ">=";
                    break;
                case "<":
                    Operator = FormulaOperator.Lt;
                    break;
                case "<=":
                case "=<":
                    Operator = FormulaOperator.Lte;
                    OperatorString = "<=";
                    break;
                default:
                    Operator = FormulaOperator.Unknown;
                    break;
            }

            if (!string.IsNullOrWhiteSpace(requiredTemplatesLeft))
            {
                RequiredTemplatesLeft = requiredTemplatesLeft.Split(',').Select(template => template.Trim()).ToList();
            }
            else
            {
                RequiredTemplatesLeft = new List<string>();
            }

            if (!string.IsNullOrWhiteSpace(requiredTemplatesRight))
            {
                RequiredTemplatesRight = requiredTemplatesRight.Split(',').Select(template => template.Trim()).ToList();
            }
            else
            {
                RequiredTemplatesRight = new List<string>();
            }

            FormulaString = $"{leftFormula} {operatorString} {rightFormula}";
            UserFriendlyFormula = userFriendlyFormula;
            _formulaResultString = $"{{0}} {operatorString} {{1}}";
            _formulaSourceString = $"{{0}} {operatorString} {{1}}";
            if (tolerance.HasValue)
            {
                //if the absolute tolerance is larger than 1, decimals beyond the second are less important for informational display
                //however, if the tolerance consists of only decimals, then display the most concise form of the number which contains all of the decimals
                string toleranceString = tolerance > 1M || tolerance < -1M
                    ? tolerance.Value.ToString("#,0.##", formatProvider)
                    : tolerance.Value.ToString("0.#############################", formatProvider);
                FormulaString = FormulaString + $" [with tolerance of {toleranceString}]";
                _formulaResultString = _formulaResultString + $" [with tolerance of {toleranceString}]";
            }
        }


        public string GetFormulaResultString(ExpressionResult leftResult, ExpressionResult rightResult,
            IFormatProvider formatter = null)
        {
            return string.Format(_formulaResultString, GetSingleResultString(leftResult, formatter),
                GetSingleResultString(rightResult, formatter));
        }

        public string GetFormulaSourceString()
        {
            return string.Format(_formulaSourceString, string.Join(",", RequiredTemplatesLeft),
                string.Join(",", RequiredTemplatesRight));
        }

        public static string GetSingleResultString(ExpressionResult result, IFormatProvider formatter = null)
        {
            formatter = formatter ?? CultureInfo.CurrentCulture;

            if (result == null || result.IsNull())
                return "NULL";
            if (result.ValueType == ValueType.Decimal)
                return result.GetDecimalValue()?.ToString("#,0.00", formatter);
            return result.ToString();
        }

        private static readonly ILogger _logger = NLog.LogManager.GetCurrentClassLogger();

        internal void ProcessFormulas()
        {
            if (!_isProcessed)
            {
                _isProcessed = true;

                try
                {
                    LeftFormulaNode = FormulaParser.ParseFormula(LeftFormula);
                }
                catch (Exception e)
                {
                    _logger.Warn(e, "Exception was thrown during formula validation.");
                }

                if (LeftFormulaNode == null)
                {
                    _logger.Error($"Could not parse formula: {LeftFormula} => {this.Code}");
                }

                try
                {
                    RightFormulaNode = FormulaParser.ParseFormula(RightFormula);
                }
                catch (Exception e)
                {
                    _logger.Warn(e, "Exception was thrown during formula validation.");
                }

                if (RightFormulaNode == null)
                {
                    _logger.Error($"Could not parse formula: {RightFormula} => {this.Code}");
                }

                if (!string.IsNullOrWhiteSpace(ConditionFormula))
                {
                    try
                    {
                        ConditionFormulaNode = FormulaParser.ParseFormula(ConditionFormula);
                    }
                    catch (Exception e)
                    {
                        _logger.Warn(e, "Exception was thrown during formula validation.");
                    }

                    if (ConditionFormulaNode == null)
                    {
                        _logger.Error($"Could not parse condition formula: {ConditionFormula} => {this.Code}");
                    }
                }
            }
        }
    }


    public enum FormulaOperator
    {
        Eq,
        Gt,
        Gte,
        Lt,
        Lte,
        Neq,
        Unknown
    }
}