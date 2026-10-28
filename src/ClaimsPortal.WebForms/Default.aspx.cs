using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace ClaimsPortal.WebForms
{
    public partial class DefaultPage : Page
    {
        public sealed class SummaryItem
        {
            public string FieldId { get; set; } = "";
            public string Message { get; set; } = "";
        }

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            ViewStateUserKey = AntiForgery.GetOrCreateToken(Context);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // Money and dates are always shown in US format, whatever the server's culture is
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

            if (!IsPostBack)
            {
                StatusFilter.Items.Add(new ListItem("Any status", ""));
                foreach (var status in LegacyClaimStore.Statuses)
                {
                    StatusFilter.Items.Add(new ListItem(status, status));
                }
                StatusFilter.SelectedIndex = 0;
            }
        }

        protected void FindButton_Click(object sender, EventArgs e)
        {
            Page.Validate();
            ShowErrors();

            if (!Page.IsValid)
            {
                Results.Visible = false;
                return;
            }

            var claims = LegacyClaimStore.Find(ClaimantId.Text.Trim(), StatusFilter.SelectedValue);
            ClaimsGrid.DataSource = claims;
            ClaimsGrid.DataBind();
            ResultCount.Text = claims.Count == 1 ? "1 claim found." : claims.Count + " claims found.";
            // The results heading has data-focus-on-load, so after the postback the user starts
            // at the results instead of the top of the page
            Results.Visible = true;
        }

        protected void ClaimsGrid_DataBound(object sender, EventArgs e)
        {
            if (ClaimsGrid.HeaderRow != null)
            {
                ClaimsGrid.HeaderRow.TableSection = TableRowSection.TableHeader;
            }
        }

        private void ShowErrors()
        {
            // The first failed validator per field wins, so each field shows one message
            var failed = Page.Validators.Cast<BaseValidator>()
                .Where(v => !v.IsValid)
                .GroupBy(v => v.ControlToValidate)
                .Select(g => new SummaryItem { FieldId = g.Key, Message = g.First().ErrorMessage })
                .ToList();

            ErrorSummary.Visible = failed.Count > 0;
            ErrorList.DataSource = failed;
            ErrorList.DataBind();

            var claimantIdError = failed.FirstOrDefault(f => f.FieldId == ClaimantId.ID);
            ClaimantIdErrorHolder.Visible = claimantIdError != null;
            if (claimantIdError != null)
            {
                // "Error:" is read out, so the problem is not shown by color alone
                ClaimantIdError.Text = "Error: " + claimantIdError.Message;
                ClaimantId.Attributes["aria-invalid"] = "true";
                ClaimantId.Attributes["aria-describedby"] = "ClaimantId-hint ClaimantId-error";
                ClaimantId.CssClass = "input--short";
            }
            else
            {
                ClaimantId.Attributes.Remove("aria-invalid");
                ClaimantId.Attributes["aria-describedby"] = "ClaimantId-hint";
            }
        }
    }
}
