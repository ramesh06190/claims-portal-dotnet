<%@ Page Title="Look up your claims" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="ClaimsPortal.WebForms.DefaultPage" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">

    <%--
        Error summary built from Page.Validators in the code behind, with a link to each field.
        The built in ValidationSummary cannot link to fields, so it is not used.
    --%>
    <asp:PlaceHolder ID="ErrorSummary" runat="server" Visible="false">
        <div class="error-summary" id="error-summary" tabindex="-1">
            <h2 id="error-summary-title">There is a problem</h2>
            <ul aria-labelledby="error-summary-title">
                <asp:Repeater ID="ErrorList" runat="server" ItemType="ClaimsPortal.WebForms.DefaultPage+SummaryItem">
                    <ItemTemplate>
                        <li><a href="#<%#: Item.FieldId %>"><%#: Item.Message %></a></li>
                    </ItemTemplate>
                </asp:Repeater>
            </ul>
        </div>
    </asp:PlaceHolder>

    <h1>Look up your claims</h1>
    <p class="lead">Enter your claimant ID to see your weekly claims.</p>

    <div class="form">
        <div class="field">
            <%-- AssociatedControlID makes asp:Label render a real <label for="ClaimantId"> --%>
            <asp:Label runat="server" AssociatedControlID="ClaimantId" Text="Claimant ID" />
            <p class="field__hint" id="ClaimantId-hint">8 digits, found on your approval letter</p>
            <asp:PlaceHolder ID="ClaimantIdErrorHolder" runat="server" Visible="false">
                <p class="field__error" id="ClaimantId-error"><asp:Literal ID="ClaimantIdError" runat="server" Mode="Encode" /></p>
            </asp:PlaceHolder>
            <asp:TextBox ID="ClaimantId" runat="server" CssClass="input--short" inputmode="numeric"
                         aria-describedby="ClaimantId-hint" MaxLength="8" />

            <%--
                Display="None" and EnableClientScript="false": the default validator renders a red
                asterisk with no text, which fails 1.4.1 and 3.3.1. Messages are shown by the code behind instead.
            --%>
            <asp:RequiredFieldValidator ID="ClaimantIdRequired" runat="server" ControlToValidate="ClaimantId"
                ErrorMessage="Enter your claimant ID" Display="None" EnableClientScript="false" />
            <asp:RegularExpressionValidator ID="ClaimantIdFormat" runat="server" ControlToValidate="ClaimantId"
                ValidationExpression="^\d{8}$" ErrorMessage="Claimant ID must be 8 digits, like 12345678"
                Display="None" EnableClientScript="false" />
        </div>

        <fieldset class="field">
            <legend>Status (optional)</legend>
            <%-- RepeatLayout="UnorderedList" avoids the layout table RadioButtonList renders by default --%>
            <asp:RadioButtonList ID="StatusFilter" runat="server" RepeatLayout="UnorderedList" CssClass="radio-list" />
        </fieldset>

        <asp:Button ID="FindButton" runat="server" Text="Find claims" CssClass="btn btn--primary" OnClick="FindButton_Click" />
    </div>

    <asp:PlaceHolder ID="Results" runat="server" Visible="false">
        <h2 id="results-title" tabindex="-1">Results</h2>
        <p><asp:Literal ID="ResultCount" runat="server" Mode="Encode" /></p>

        <div class="table-wrap" role="region" aria-labelledby="results-title" tabindex="0">
            <%--
                UseAccessibleHeader renders <th scope="col"> instead of <td> for headers, and the
                code behind sets TableSection so the header row goes in a <thead>.
            --%>
            <asp:GridView ID="ClaimsGrid" runat="server" AutoGenerateColumns="false" UseAccessibleHeader="true"
                CssClass="table" GridLines="None" Caption="Your weekly claims" CaptionAlign="NotSet"
                ItemType="ClaimsPortal.WebForms.LegacyClaim" OnDataBound="ClaimsGrid_DataBound">
                <Columns>
                    <asp:BoundField DataField="ClaimNumber" HeaderText="Claim number" />
                    <asp:BoundField DataField="WeekEnding" HeaderText="Week ending" DataFormatString="{0:MMM d, yyyy}" />
                    <asp:BoundField DataField="Status" HeaderText="Status" />
                    <asp:BoundField DataField="WeeklyAmount" HeaderText="Weekly amount" DataFormatString="{0:C0}"
                        ItemStyle-CssClass="num" HeaderStyle-CssClass="num" />
                </Columns>
            </asp:GridView>
        </div>
    </asp:PlaceHolder>

</asp:Content>
