// Moves focus after a postback: to the error summary when there are errors, or to the results
// heading after a search. Only one of them is rendered at a time. Kept in a file, not written
// into the page, so the content security policy can block all inline script.
document.querySelector("[data-focus-on-load]")?.focus();
