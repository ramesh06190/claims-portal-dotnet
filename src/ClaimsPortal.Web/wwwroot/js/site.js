// Moves focus to the error summary after a failed form submit, so keyboard and
// screen reader users start at the list of problems. Everything else works without JavaScript.
document.querySelector("[data-focus-on-load]")?.focus();
