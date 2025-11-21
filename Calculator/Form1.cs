
// Velislav Ivanov Kochev, F113048

using System.Runtime.InteropServices;
using System.Web;

namespace Simple_Windows_Calculator
{
    /// <summary>
    /// Represents a simple Windows Forms calculator that supports
    /// basic arithmetic operations, special operations (square, square root,
    /// percentage, inverse) and keyboard input.
    /// </summary>
    public partial class SimpleCalculator : Form
    {
        /// <summary>
        /// Imports the HideCaret function from user32.dll.
        /// Used to hide the blinking text cursor in the display TextBox.
        /// </summary>
        [DllImport("user32.dll")]
        private static extern bool HideCaret(IntPtr hWnd);

        /// <summary>
        /// Stores the operands used in binary operations.
        /// operand[0] – first operand, operand[1] – second operand.
        /// Double.MaxValue is used as a sentinel value (uninitialized).
        /// </summary>
        private readonly double[] operand = [Double.MaxValue, Double.MaxValue];

        /// <summary>
        /// Stores the last calculated result.
        /// Double.MaxValue means that no result has been calculated yet.
        /// </summary>
        private double result = Double.MaxValue;

        /// <summary>
        /// Stores the currently pending special operation (√, %, 1/x, sqr),
        /// so that it can be incorporated when the equal button is pressed.
        /// </summary>
        private string specialOperation = "";

        /// <summary>
        /// Represents possible calculator error types.
        /// </summary>
        private enum ErrorCode
        {
            None,
            DivideByZero,
            NegativeSquareRoot
        }

        /// <summary>
        /// Error messages corresponding to the ErrorCode values.
        /// </summary>
        private readonly string[] errorText =
        {
            "No Error",
            "Cannot divide by zero",
            "Invalid input"
        };

        /// <summary>
        /// Numeric formatting pattern used for all displayed values.
        /// Ensures no trailing zeros and no scientific notation.
        /// </summary>
        private const string precisionFormat = "##############0.##############";

        /// <summary>
        /// Tracks whether the result should be cleared only once after an operation.
        /// Used to prevent automatically chaining values when the user starts a new input.
        /// </summary>
        private bool clearFirstTime = false;

        /// <summary>
        /// Initializes a new instance of the SimpleCalculator form.
        /// </summary>
        public SimpleCalculator()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Form Load event handler.
        /// Currently not used, but can be used to initialize state
        /// when the form is first shown.
        /// </summary>
        private void Form1_Load(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// MouseDown event handler for the main display TextBox.
        /// Hides the blinking caret so that the display looks more like
        /// a read-only calculator display.
        /// </summary>
        private void MainDisplay_MouseDown(object sender, MouseEventArgs e)
        {
            HideCaret(textMainDisplay.Handle);
        }

        /// <summary>
        /// Click event handler for all numeric buttons (0–9).
        /// Appends the pressed digit to the main display,
        /// enforcing maximum length and handling leading zeros and result overwrites.
        /// </summary>
        private void ButtonNumbers_Click(object sender, EventArgs e)
        {
            EnableOperationKeys(sender, e);
            Button button = (Button)sender;

            // If the currently displayed value is the last result and we haven't cleared once yet,
            // clear the entry before starting to type the new number.
            if (textMainDisplay.Text == result.ToString(precisionFormat) && clearFirstTime == false)
            {
                btnClearEntry.PerformClick();
                clearFirstTime = true;
            }

            // Limit the number of characters in the display.
            // We allow an extra character if there is a decimal point and/or a minus sign.
            if (textMainDisplay.TextLength < 13
                + Convert.ToInt32(textMainDisplay.Text.Contains(btnDot.Text))
                + Convert.ToInt32(textMainDisplay.Text.Contains("-")))
            {
                if (textMainDisplay.Text.Equals("0"))
                {
                    textMainDisplay.Text = button.Text;
                }
                else
                {
                    textMainDisplay.Text += button.Text;
                }
            }
        }

        /// <summary>
        /// Click event handler for the decimal point button.
        /// Appends the decimal separator if it is not already present
        /// in the current value.
        /// </summary>
        private void ButtonDot_Click(object sender, EventArgs e)
        {
            Button btnDot = (Button)sender;
            //MessageBox.Show("Button Dot clicked");
            if (textMainDisplay.Text.Contains(btnDot.Text) == false)
            {
                textMainDisplay.Text += btnDot.Text;
            }
        }

        /// <summary>
        /// Click event handler for the +/- button.
        /// Toggles the sign of the current value in the main display,
        /// unless the value is zero.
        /// </summary>
        private void ButtonPlusMinus_Click(object sender, EventArgs e)
        {
            clearFirstTime = false;
            if (double.Parse(textMainDisplay.Text) != 0.0)
            {
                textMainDisplay.Text = (Convert.ToDouble(textMainDisplay.Text) * -1.0)
                    .ToString(precisionFormat);
            }
        }

        /// <summary>
        /// Click event handler for the C (Clear) button.
        /// Resets the main display and clears the formula,
        /// but does not reset the internal operand/result state.
        /// </summary>
        private void ButtonClear_Click(object sender, EventArgs e)
        {
            EnableOperationKeys(sender, e);
            textMainDisplay.Text = "0";
            textFormulaDisplay.Clear();
        }

        /// <summary>
        /// Click event handler for the CE (Clear Entry) button.
        /// Clears only the current entry in the main display.
        /// If a completed expression (with '=') is present in the formula,
        /// it is cleared as well.
        /// </summary>
        private void ButtonClearEntry_Click(object sender, EventArgs e)
        {
            EnableOperationKeys(sender, e);
            if (textFormulaDisplay.Text.Contains("="))
            {
                textFormulaDisplay.Clear();
            }
            textMainDisplay.Text = "0";
        }

        /// <summary>
        /// Click event handler for the backspace button.
        /// Deletes the last character from the main display.
        /// If only one character remains, the display is reset to zero.
        /// </summary>
        private void ButtonBack_Click(object sender, EventArgs e)
        {
            EnableOperationKeys(sender, e);
            if (textMainDisplay.Text.Length == 1)
            {
                textMainDisplay.Text = "0";
            }
            else
            {
                textMainDisplay.Text = textMainDisplay.Text.Remove(textMainDisplay.Text.Length - 1);
                StandardizeMainDisplay(sender, e);
            }
        }

        /// <summary>
        /// Applies the pending basic arithmetic operation (+, -, ×, ÷)
        /// to the operands and stores the result in operand[0].
        /// </summary>
        /// <returns>
        /// An ErrorCode value indicating whether an error occurred
        /// (for example divide by zero) or not.
        /// </returns>
        private ErrorCode ApplyBasicOperation()
        {
            // Detect which basic operation is currently used by inspecting the formula display.

            if (textFormulaDisplay.Text.Contains("+"))
            {
                operand[0] += operand[1];
            }
            else if (textFormulaDisplay.Text.Contains("-")) // The minus sign here is a special character, so be careful.
            {
                operand[0] -= operand[1];
            }
            else if (textFormulaDisplay.Text.Contains("×"))
            {
                operand[0] *= operand[1];
            }
            else if (textFormulaDisplay.Text.Contains("÷"))
            {
                if (operand[1] == 0)
                {
                    //MessageBox.Show("Check var");
                    return ErrorCode.DivideByZero;
                }
                operand[0] /= operand[1];
            }
            else
            {
                // No previous operator: treat operand[1] as the first operand.
                operand[0] = operand[1];
            }
            return ErrorCode.None;
        }

        /// <summary>
        /// Click event handler for basic operation buttons (+, -, ×, ÷).
        /// Updates the operands, applies the previous pending operation if needed,
        /// and prepares the formula display for the next input.
        /// </summary>
        private void ButtonBasicOperation_Click(object sender, EventArgs e)
        {
            clearFirstTime = false;
            Button button = (Button)sender;

            // The result currently displayed on MainDisplay matches the result stored in the temporary variable.
            // The two results are the same, so push it up to the FormulaDisplay.
            double mainDisplayValue = Double.Parse(textMainDisplay.Text);
            if (mainDisplayValue == result)
            {
                operand[0] = result;
            }
            else if (textFormulaDisplay.Text != String.Empty)
            {
                operand[1] = mainDisplayValue;
                if (ApplyBasicOperation() == ErrorCode.DivideByZero)
                {
                    HandleInvalidInput(ErrorCode.DivideByZero);
                    return;
                }
                textMainDisplay.Text = operand[0].ToString(precisionFormat);
            }
            else
            {
                // The case where textFormulaDisplay is empty: this is the first operand.
                operand[0] = mainDisplayValue;
            }

            // Show the current operand and operator in the formula display.
            textFormulaDisplay.Text = operand[0].ToString(precisionFormat) + " " + button.Text;
            textMainDisplay.Text = "0";
        }

        /// <summary>
        /// Click event handler for special operation buttons:
        /// square, square root, inverse (1/x) and percentage.
        /// Calculates the intermediate result and then triggers the equal button.
        /// </summary>
        private void ButtonSpecialOperation_Click(object sender, EventArgs e)
        {
            clearFirstTime = false;
            Button button = (Button)sender;
            string operationName = button.Name;
            double mainDisplayValue = Double.Parse(textMainDisplay.Text);

            switch (operationName)
            {
                case "btnSquare":
                    textMainDisplay.Text = (mainDisplayValue * mainDisplayValue)
                        .ToString(precisionFormat);
                    specialOperation = "sqr";
                    break;

                case "btnSquareRoot":
                    if (mainDisplayValue < 0)
                    {
                        HandleInvalidInput(ErrorCode.NegativeSquareRoot);
                        return;
                    }
                    textMainDisplay.Text = Math.Sqrt(mainDisplayValue)
                        .ToString(precisionFormat);
                    specialOperation = "√";
                    break;

                case "btnInverse":
                    if (mainDisplayValue == 0)
                    {
                        HandleInvalidInput(ErrorCode.DivideByZero);
                        return;
                    }
                    textMainDisplay.Text = (1.0 / mainDisplayValue)
                        .ToString(precisionFormat);
                    specialOperation = "1/";
                    break;

                case "btnPercent":
                    if (operand[0] != Double.MaxValue)
                    {
                        textMainDisplay.Text = (operand[0] * mainDisplayValue / 100.0)
                            .ToString(precisionFormat);
                    }
                    else
                    {
                        textMainDisplay.Text = "0";
                    }
                    specialOperation = "%";
                    break;

                default:
                    break;
            }

            // Store the original value for use in the formula text.
            result = mainDisplayValue;
            btnEqual.PerformClick();
        }

        /// <summary>
        /// Click event handler for the = (equal) button.
        /// Finalizes the current calculation, applies any pending special operation,
        /// updates the formula display, and shows the result.
        /// </summary>
        private void ButtonEqual_Click(object sender, EventArgs e)
        {
            EnableOperationKeys(sender, e);

            // Only recalculate if the display value is new or zero.
            if (textMainDisplay.Text != result.ToString(precisionFormat) || textMainDisplay.Text == "0")
            {
                operand[1] = Double.Parse(textMainDisplay.Text);

                // If textFormulaDisplay is not empty and it doesn't contain an equals sign,
                // then consider that the expression has not been completed yet.
                if (textFormulaDisplay.Text != String.Empty && !textFormulaDisplay.Text.Contains('='))
                {
                    ErrorCode error = ApplyBasicOperation();
                    if (error != ErrorCode.None)
                    {
                        HandleInvalidInput(error);
                        return;
                    }

                    if (specialOperation != String.Empty)
                    {
                        if (specialOperation.Equals("%"))
                        {
                            double displayValue = operand[0] != Double.MaxValue ? operand[0] : 0.0;
                            textFormulaDisplay.Text += $" ({displayValue}) × {result}% = ";
                        }
                        else
                        {
                            textFormulaDisplay.Text += " " + specialOperation + "(" +
                                                       result.ToString(precisionFormat) + ") =";
                        }
                    }
                    else
                    {
                        textFormulaDisplay.Text += " " + operand[1].ToString(precisionFormat) + " =";
                    }

                    result = operand[0];
                    textMainDisplay.Text = result.ToString(precisionFormat);
                    specialOperation = "";
                }
                else if (specialOperation != String.Empty)
                {
                    // This case occurs when the expression already has an equals sign
                    // and the user presses a special function key again.
                    textFormulaDisplay.Clear();

                    if (specialOperation.Equals("%"))
                    {
                        double displayValue = operand[0] != Double.MaxValue ? operand[0] : 0.0;
                        textFormulaDisplay.Text = $"({displayValue}) × {result}% = ";
                    }
                    else
                    {
                        textFormulaDisplay.Text = $"{specialOperation}({result}) = ";
                    }

                    specialOperation = "";
                    result = operand[1];
                }
            }
        }

        /// <summary>
        /// Resets all internal calculation values (operands and result)
        /// to their initial sentinel state.
        /// </summary>
        private void ResetCalculationValues()
        {
            operand[0] = operand[1] = result = Double.MaxValue;
        }

        /// <summary>
        /// Disables all operation buttons.
        /// Used when an invalid input occurs so the user cannot continue
        /// until the error is cleared.
        /// </summary>
        private void DisableOperationKeys()
        {
            btnAdd.Enabled = false;
            btnSubtract.Enabled = false;
            btnMultiply.Enabled = false;
            btnDivide.Enabled = false;
            btnPercent.Enabled = false;
            btnInverse.Enabled = false;
            btnSquare.Enabled = false;
            btnSquareRoot.Enabled = false;
            btnPlusMinus.Enabled = false;
            btnDot.Enabled = false;
        }

        /// <summary>
        /// Handles invalid user input such as division by zero
        /// or negative square root. Shows an error message and
        /// disables further operations until reset.
        /// </summary>
        /// <param name="errorCode">The error type to be displayed.</param>
        private void HandleInvalidInput(ErrorCode errorCode)
        {
            ResetCalculationValues();
            DisableOperationKeys();
            textMainDisplay.Text = errorText[(int)errorCode];
            textFormulaDisplay.Clear();
        }

        /// <summary>
        /// Enables all operation buttons if they are currently disabled.
        /// Also resets the main display to "0" when re-enabling.
        /// Typically called whenever the user starts a new valid input.
        /// </summary>
        private void EnableOperationKeys(object sender, EventArgs e)
        {
            if (btnAdd.Enabled)
            {
                return;
            }

            btnAdd.Enabled = true;
            btnSubtract.Enabled = true;
            btnMultiply.Enabled = true;
            btnDivide.Enabled = true;
            btnPercent.Enabled = true;
            btnInverse.Enabled = true;
            btnSquare.Enabled = true;
            btnSquareRoot.Enabled = true;
            btnPlusMinus.Enabled = true;
            btnDot.Enabled = true;
            textMainDisplay.Text = "0";
        }

        /// <summary>
        /// Normalizes the content of the main display to a standard numeric format.
        /// If parsing fails, the value is reset to zero.
        /// </summary>
        private void StandardizeMainDisplay(object sender, EventArgs e)
        {
            if (Double.TryParse(textMainDisplay.Text, out double standardValue))
            {
                textMainDisplay.Text = standardValue.ToString(precisionFormat);
            }
            else
            {
                textMainDisplay.Text = "0";
            }
        }

        /// <summary>
        /// Handles KeyPress events for the form.
        /// Maps digit and backspace keys directly to the corresponding buttons.
        /// Remember to enable KeyPreview on the form for this to work.
        /// </summary>
        private void SimpleCalculator_KeyPress(object sender, KeyPressEventArgs e)
        {
            switch (e.KeyChar)
            {
                case (char)Keys.D0:
                case (char)Keys.NumPad0:
                    btnNum0.PerformClick();
                    break;
                case (char)Keys.D1:
                case (char)Keys.NumPad1:
                    btnNum1.PerformClick();
                    break;
                case (char)Keys.D2:
                case (char)Keys.NumPad2:
                    btnNum2.PerformClick();
                    break;
                case (char)Keys.D3:
                case (char)Keys.NumPad3:
                    btnNum3.PerformClick();
                    break;
                case (char)Keys.D4:
                case (char)Keys.NumPad4:
                    btnNum4.PerformClick();
                    break;
                case (char)Keys.D5:
                case (char)Keys.NumPad5:
                    btnNum5.PerformClick();
                    break;
                case (char)Keys.D6:
                case (char)Keys.NumPad6:
                    btnNum6.PerformClick();
                    break;
                case (char)Keys.D7:
                case (char)Keys.NumPad7:
                    btnNum7.PerformClick();
                    break;
                case (char)Keys.D8:
                case (char)Keys.NumPad8:
                    btnNum8.PerformClick();
                    break;
                case (char)Keys.D9:
                case (char)Keys.NumPad9:
                    btnNum9.PerformClick();
                    break;
                case (char)Keys.Back:
                    btnBack.PerformClick();
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Handles KeyDown events for the form.
        /// Maps common operation keys (+, -, *, /, ., Delete) from the keyboard
        /// to the corresponding calculator buttons.
        /// </summary>
        private void SimpleCalculator_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Add)
            {
                btnAdd.PerformClick();
            }
            else if (e.KeyCode == Keys.OemMinus || e.KeyCode == Keys.Subtract)
            {
                btnSubtract.PerformClick();
            }
            else if (e.KeyCode == Keys.Multiply || (e.Shift && e.KeyCode == Keys.D8))
            {
                btnMultiply.PerformClick();
            }
            else if (e.KeyCode == Keys.Divide || e.KeyCode == Keys.OemQuestion)
            {
                btnDivide.PerformClick();
            }
            else if (e.KeyCode == Keys.OemPeriod || e.KeyCode == Keys.Decimal)
            {
                btnDot.PerformClick();
            }
            else if (e.KeyCode == Keys.Delete)
            {
                btnClearEntry.PerformClick();
            }
        }

        /// <summary>
        /// Overrides ProcessCmdKey to intercept the Enter key before
        /// the normal KeyPress event. This ensures that Enter is always
        /// routed to the equal button, even if the active control would
        /// otherwise "swallow" the key.
        /// </summary>
        /// <param name="msg">The message being processed.</param>
        /// <param name="keyData">Information about the pressed key.</param>
        /// <returns>
        /// True if the key was processed; otherwise calls the base implementation.
        /// </returns>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter && this.ActiveControl != null)
            {
                if (this.ActiveControl.Handle != this.btnEqual.Handle)
                {
                    this.btnEqual.Select();
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
