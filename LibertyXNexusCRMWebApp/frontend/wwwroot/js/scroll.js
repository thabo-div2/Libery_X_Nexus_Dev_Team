/// <summary>
/// Scrolls a box all the way to the bottom.
/// </summary>
window.scrollToBottom = (element) => {
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
};

/// <summary>
/// Scrolls to the first field with an error and puts the cursor in it.
/// </summary>
window.focusFirstError = () => {
    const field = document.querySelector('.has-error');
    if (!field) {
        return;
    }

    field.scrollIntoView({ behavior: 'smooth', block: 'center' });

    const input = field.querySelector('input:not([type="hidden"]), select, textarea, button');
    if (input) {
        input.focus({ preventScroll: true });
    }
};

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
