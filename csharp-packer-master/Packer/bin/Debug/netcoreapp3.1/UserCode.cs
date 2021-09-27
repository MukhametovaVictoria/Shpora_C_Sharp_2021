using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace RandomVariable
{
    public enum StatisticKind
    {
        ExpectedValue,
        Variance,
        ProbabilityDistribution,
    }
}

namespace RandomVariable
{
    public class RandomVariableStatisticCalculator : IRandomVariableStatisticCalculator
    {
        RandomVariableStatistic RVS { get; set; }
        private static List<Tuple<Numbers, char>> list;
        public RandomVariableStatisticCalculator()
        {
            RVS = new RandomVariableStatistic();
        }

        public RandomVariableStatistic CalculateStatistic(string expression, params StatisticKind[] statisticForCalculate)
        {
            ParseExpression(expression);
            if (statisticForCalculate.Contains(StatisticKind.ExpectedValue))
                RVS.ExpectedValue = ResolveMathExpORVariance("ExpectedValue").DNumber;
            if(statisticForCalculate.Contains(StatisticKind.Variance))
                RVS.Variance = ResolveMathExpORVariance("Variance").DNumber;
            if (statisticForCalculate.Contains(StatisticKind.ProbabilityDistribution))
                RVS.ProbabilityDistribution = ResolveProbability();
            return RVS;
        }

        private static void ParseExpression(string str)
        {
            list = new List<Tuple<Numbers, char>>();
            var stackOfOps = new Stack<char>();
            var sb = new StringBuilder();
            var simple = true;
            for (int i = 0; i < str.Length; i++)
            {
                if (Char.IsDigit(str[i]) || str[i] == 'd' || str[i] == '.')
                {
                    sb.Append(str[i]);
                    if (str[i] == 'd') simple = false;
                }
                else
                {
                    if (sb.Length != 0)
                        AddToList(simple, sb.ToString());
                    else if (str[i] == '-' && (i - 1 < 0 || str[i - 1] == '('))
                    {
                        sb.Append(str[i]);
                        continue;
                    }

                    if (str[i] == '(')
                        stackOfOps.Push(str[i]);
                    else if (str[i] == ')')
                    {
                        char s = stackOfOps.Pop();

                        while (s != '(')
                        {
                            list.Add(new Tuple<Numbers, char>(null, s));
                            s = stackOfOps.Pop();
                        }
                    }
                    else
                    {
                        if (stackOfOps.Count > 0)
                            if (GetPriority(str[i]) <= GetPriority(stackOfOps.Peek()))
                                list.Add(new Tuple<Numbers, char>(null, stackOfOps.Pop()));

                        stackOfOps.Push(str[i]);

                    }
                    simple = true;
                    sb.Clear();
                }
            }

            if (sb.Length > 0)
                AddToList(simple, sb.ToString());
            while (stackOfOps.Count > 0)
                list.Add(new Tuple<Numbers, char>(null, stackOfOps.Pop()));
        }

        private static void AddToList(bool simple, string value)
        {
            var number = new Numbers(value, simple);
            list.Add(new Tuple<Numbers, char>(number, ' '));
        }

        static private byte GetPriority(char s)
        {
            switch (s)
            {
                case '(': return 0;
                case ')': return 1;
                case '+': return 2;
                case '-': return 3;
                case '*': return 4;
                case '/': return 4;
                case '^': return 5;
                default: return 6;
            }
        }

        private static Numbers ResolveMathExpORVariance(string statisticForCalculate)
        {
            var stack = new Stack<Numbers>();
            if (list.Count == 1)
            {
                var num = new Numbers(list[0].Item1.Number, list[0].Item1.Simple);
                if (statisticForCalculate == "ExpectedValue")
                {
                    num.FindMathExpectOfRandExp();
                    return num;
                }
                else if (statisticForCalculate == "Variance")
                {
                    num.FindVarianceOfRandExp();
                    return num;
                }
            }
            else
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Item1 != null) stack.Push(list[i].Item1);
                    else
                    {
                        var num2 = stack.Pop();
                        var num1 = stack.Pop();
                        var number1 = new Numbers(num1.Number, num1.Simple) { Resolved = num1.Resolved, DNumber = num1.DNumber };
                        var number2 = new Numbers(num2.Number, num2.Simple) { Resolved = num2.Resolved, DNumber = num2.DNumber };
                        if (statisticForCalculate == "ExpectedValue")
                        {
                            if (!number1.Resolved) number1.FindMathExpectOfRandExp();
                            if (!number2.Resolved) number2.FindMathExpectOfRandExp();
                            var newNum = FindMathExp(number1, number2, list[i].Item2);
                            stack.Push(newNum);
                        }
                        else if (statisticForCalculate == "Variance")
                        {
                            if (!number1.Resolved) number1.FindVarianceOfRandExp();
                            if (!number2.Resolved) number2.FindVarianceOfRandExp();
                            var newNum = FindVariance(number1, number2, list[i].Item2);
                            stack.Push(newNum);
                        }
                    }
                }
            }
            return stack.Pop();
        }

        private static Numbers FindMathExp(Numbers number1, Numbers number2, char op)
        {
            if (!number1.Simple && !number2.Simple && (op == '*' || op == '/'))
                throw new Exception("������ ��������/������ ��������� ��������� ���� �� �����");
            var value = Find(number1.DNumber, number2.DNumber, op);
            var newNumber = new Numbers(value.ToString(),
                    !number1.Simple ? number1.Simple : !number2.Simple ? number2.Simple : true);
            newNumber.DNumber = value;
            newNumber.Resolved = true;
            return newNumber;
        }

        private static Numbers FindVariance(Numbers number1, Numbers number2, char op)
        {
            if (!number1.Simple && !number2.Simple && (op == '*' || op == '/'))
                throw new Exception("������ ��������/������ ��������� ��������� ���� �� �����");
            var value = 0.0;
            if (op == '*' || op == '/')
            {
                if (!number1.Simple || !number2.Simple) value = Find(number1.Simple ? number1.DNumber * number1.DNumber : number1.DNumber,
                number2.Simple ? number2.DNumber * number2.DNumber : number2.DNumber, op);
            }
            if (op == '+' || op == '-')
            {
                if (!number1.Simple && !number2.Simple)
                    value = number1.DNumber + number2.DNumber;
                else if (!number1.Simple && number2.Simple)
                    value = number1.DNumber;
                else if (number1.Simple && !number2.Simple)
                    value = number2.DNumber;
            }
            return new Numbers(value.ToString(), !number1.Simple ? number1.Simple : !number2.Simple ? number2.Simple : true)
            {
                DNumber = value,
                Resolved = true
            };
        }

        private static double Find(double a, double b, char op)
        {
            switch (op)
            {
                case '+':
                    return a + b;

                case '-':
                    return a - b;

                case '*':
                    return a * b;

                case '/':
                    return a / b;

                default:
                    throw new FormatException(string.Format("�������� ������� ������"));
            }
        }

        private static Dictionary<double, double> ResolveProbability()
        {
            var stack = new Stack<Tuple<Numbers, Dictionary<double, double>>>();
            if (list.Count == 1)
            {
                if (list[0].Item1.Simple) return new Dictionary<double, double>() { [list[0].Item1.DNumber] = 1.0 };
                else
                {
                    FindProbabilityForOneDice(list[0].Item1.Number);
                    return probabilities;
                }
            }
            else
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Item1 != null) stack.Push(new Tuple<Numbers, Dictionary<double, double>>(list[i].Item1, null));
                    else
                    {
                        var number2 = stack.Pop();
                        var number1 = stack.Pop();
                        Dictionary<double, double> dic1 = new Dictionary<double, double>();
                        Dictionary<double, double> dic2 = new Dictionary<double, double>();
                        if (number1.Item2 == null && number1.Item1.Simple)
                            dic1[number1.Item1.DNumber] = 1.0;
                        else if (number1.Item2 == null && !number1.Item1.Simple)
                        {
                            FindProbabilityForOneDice(number1.Item1.Number);
                            dic1 = probabilities;
                        }
                        else dic1 = number1.Item2;

                        if (number2.Item2 == null && number2.Item1.Simple)
                            dic2[number2.Item1.DNumber] = 1.0;
                        else if (number2.Item2 == null && !number2.Item1.Simple)
                        {
                            FindProbabilityForOneDice(number2.Item1.Number);
                            dic2 = probabilities;
                        }
                        else dic2 = number2.Item2;

                        if (!number1.Item1.Simple && !number2.Item1.Simple)
                            FindProbabilityForDices(dic1, dic2, list[i].Item2);
                        else if (!number1.Item1.Simple && number2.Item1.Simple)
                            FindProbabilityForDiceAndSimple(dic1, dic2.Keys.First(), list[i].Item2, true);
                        else if (number1.Item1.Simple && !number2.Item1.Simple)
                            FindProbabilityForDiceAndSimple(dic2, dic1.Keys.First(), list[i].Item2, false);
                        else
                            FindProbabilityForSimples(dic1.Keys.First(), dic2.Keys.First(), list[i].Item2);

                        var newDic = probabilities;
                        stack.Push(new Tuple<Numbers, Dictionary<double, double>>(
                            new Numbers("0", !number1.Item1.Simple ? false : !number2.Item1.Simple ? false : true), newDic));
                    }
                }
            }
            return stack.Pop().Item2;
        }


        private static Dictionary<double, double> probabilities;

        private static void FindProbabilityForOneDice(string number)
        {
            probabilities = new Dictionary<double, double>();
            var arr = number.Split('d');
            var count = double.Parse(arr[0], CultureInfo.InvariantCulture);
            var value = double.Parse(arr[1], CultureInfo.InvariantCulture);
            var iters = (int)Math.Abs(count);
            FindProbabilityForOneDice(iters, new double[iters], value, count);
        }

        private static void FindProbabilityForOneDice(int iters, double[] ind, double value, double count)
        {
            for (int j = 1; j <= value; j++)
            {
                ind[iters - 1] = j;
                if (iters - 1 > 0)
                {
                    FindProbabilityForOneDice(iters - 1, ind, value, count);
                    continue;
                }
                var sum = 0.0;
                if (count < 0) sum = ind.Sum() * (-1);
                else sum = ind.Sum();
                if (!probabilities.ContainsKey(sum)) probabilities[sum] = 0.0;
                probabilities[sum] += 1 / Math.Pow(value, Math.Abs(count));
            }
        }

        private static void FindProbabilityForDices(Dictionary<double, double> dic1, Dictionary<double, double> dic2, char op)
        {
            probabilities = new Dictionary<double, double>();
            for (int i = 0; i < dic1.Count(); i++)
            {
                for (int j = 0; j < dic2.Count(); j++)
                {
                    var num1 = 0.0;
                    if (op == '+') num1 = dic1.Keys.ElementAt(i) + dic2.Keys.ElementAt(j);
                    else num1 = dic1.Keys.ElementAt(i) - dic2.Keys.ElementAt(j);
                    var num2 = dic1.Values.ElementAt(i) * dic2.Values.ElementAt(j);
                    if (!probabilities.ContainsKey(num1)) probabilities[num1] = 0.0;
                    probabilities[num1] += num2;
                }
            }
        }

        private static void FindProbabilityForDiceAndSimple(Dictionary<double, double> dic, double number, char op, bool firstIsDic)
        {
            probabilities = new Dictionary<double, double>();
            for (int i = 0; i < dic.Count(); i++)
            {
                var num = 0.0;
                if (firstIsDic) num = Find(dic.Keys.ElementAt(i), number, op);
                else num = Find(number, dic.Keys.ElementAt(i), op);
                probabilities[num] = dic.Values.ElementAt(i);
            }
        }

        private static void FindProbabilityForSimples(double number1, double number2, char op)
        {
            probabilities = new Dictionary<double, double>();
            var val = Find(number1, number2, op);
            probabilities[val] = 1.0;
        }
    }

    public class Numbers
    {
        public string Number { get; set; }
        public bool Simple { get; }

        public double DNumber { get; set; }

        public bool Resolved { get; set; }
        public Numbers(string number, bool simple)
        {
            Number = number;
            Simple = simple;
            if (Simple) DNumber = double.Parse(Number, CultureInfo.InvariantCulture);
        }

        public void FindMathExpectOfRandExp()
        {
            if (!Simple)
            {
                var arr = Number.Split('d');
                var count = double.Parse(arr[0], CultureInfo.InvariantCulture);
                var value = double.Parse(arr[1], CultureInfo.InvariantCulture);
                double sum = 0;
                for (double i = 1; i <= value; i++)
                    sum += i;
                DNumber = count * sum / value;
                Number = DNumber.ToString();
            }
            Resolved = true;
        }

        public void FindVarianceOfRandExp()
        {
            if (!Simple)
            {
                var arr = Number.Split('d');
                var count = double.Parse(arr[0], CultureInfo.InvariantCulture);
                var value = double.Parse(arr[1], CultureInfo.InvariantCulture);
                double expected = 0;
                double sum = 0;
                for (double i = 1; i <= value; i++)
                    expected += i;
                expected /= value;
                for (double i = 1; i <= value; i++)
                    sum += (i - expected) * (i - expected);
                DNumber = Math.Abs(count) * sum / value;
                Number = DNumber.ToString();
            }
            Resolved = true;
        }
    }
}

namespace RandomVariable
{
    public class RandomVariableStatistic
    {
        public double? ExpectedValue { get; set; }
        public double? Variance { get; set; }
        public Dictionary<double, double> ProbabilityDistribution { get; set; }
    }
}

namespace RandomVariable
{
    public interface IRandomVariableStatisticCalculator
    {
        RandomVariableStatistic CalculateStatistic(string expression, params StatisticKind[] statisticForCalculate);
    }
}

