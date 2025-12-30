document.addEventListener('DOMContentLoaded', function () {
    function updateValidationClasses() {
        const allFields = document.querySelectorAll('input, select, textarea');
        allFields.forEach(function (field) {
            const fieldName = field.name || field.id;
            if (!fieldName) {
                return;
            }
            const validationSpan = document.querySelector(`span[data-valmsg-for="${fieldName}"]`);
            const hasError = validationSpan && validationSpan.textContent.trim() !== '';
            if (hasError) {
                field.classList.add('is-invalid');
            } else {
                field.classList.remove('is-invalid');
            }
        });
    }

    const observer = new MutationObserver(function (mutations) {
        updateValidationClasses();

        const validationSpans = document.querySelectorAll('span[data-valmsg-for]');
        const hasErrors = Array.from(validationSpans).some(span => span.textContent.trim() !== '');

        if (hasErrors) {
            const forms = document.querySelectorAll('form');
            forms.forEach(function (form) {
                resetSubmitButton(form);
            });
        }
    });

    observer.observe(document.body, {
        childList: true,
        subtree: true,
        characterData: true
    });

    updateValidationClasses();

    function resetSubmitButton(form) {
        const submitButton = form.querySelector('button[type="submit"], input[type="submit"]');
        if (submitButton && submitButton.dataset.submitting === 'true') {
            const spinner = submitButton.querySelector('.spinner-border');
            if (spinner) {
                spinner.remove();
            }

            submitButton.disabled = false;
            delete submitButton.dataset.submitting;
        }
    }

    const forms = document.querySelectorAll('form');
    forms.forEach(function (form) {
        form.addEventListener('submit', function (event) {
            const submitButton = form.querySelector('button[type="submit"], input[type="submit"]');

            if (submitButton) {
                if (submitButton.dataset.submitting === 'true') {
                    event.preventDefault();
                    return;
                }

                submitButton.dataset.submitting = 'true';
                submitButton.disabled = true;

                const spinnerHtml = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> ';
                submitButton.insertAdjacentHTML('afterbegin', spinnerHtml);
            }
        });
    });
});

function initCustomSelect(prefix) {
    const display = document.getElementById(prefix + 'Display');
    const dropdown = document.getElementById(prefix + 'Dropdown');
    const search = document.getElementById(prefix + 'Search');
    const optionsList = document.getElementById(prefix + 'Options');
    const select = document.getElementById(prefix + 'Select');
    const wrapper = document.getElementById(prefix + 'Wrapper');

    if (!display || !dropdown || !search || !optionsList || !select) return;

    Array.from(select.options).forEach(opt => {
        if (opt.value === '') return;

        const li = document.createElement('li');
        li.className = 'custom-select-option';
        li.textContent = opt.text;
        li.setAttribute('data-value', opt.value);

        li.addEventListener('click', function () {
            selectOption(opt.value, opt.text);
        });

        optionsList.appendChild(li);
    });

    if (select.value) {
        const selectedOption = select.options[select.selectedIndex];
        display.value = selectedOption.text;
    }

    display.addEventListener('click', function (e) {
        e.stopPropagation();
        const isOpen = dropdown.classList.contains('show');
        closeAllDropdowns();
        if (!isOpen) {
            dropdown.classList.add('show');
            search.value = '';
            filterOptions('');
            search.focus();
        }
    });

    search.addEventListener('input', function () {
        filterOptions(this.value);
    });

    search.addEventListener('click', function (e) {
        e.stopPropagation();
    });

    function selectOption(value, text) {
        select.value = value;
        display.value = text;
        dropdown.classList.remove('show');
        updateSelectedState();
        $(select).valid();
        syncValidationState();
    }

    function filterOptions(searchTerm) {
        const term = searchTerm.toLowerCase();
        const options = optionsList.querySelectorAll('.custom-select-option');

        options.forEach(opt => {
            const text = opt.textContent.toLowerCase();
            if (text.includes(term)) {
                opt.classList.remove('hidden');
            } else {
                opt.classList.add('hidden');
            }
        });
    }

    function updateSelectedState() {
        const options = optionsList.querySelectorAll('.custom-select-option');
        options.forEach(opt => {
            if (opt.getAttribute('data-value') === select.value) {
                opt.classList.add('selected');
            } else {
                opt.classList.remove('selected');
            }
        });
    }

    function syncValidationState() {
        if ($(select).hasClass('input-validation-error')) {
            display.classList.add('is-invalid');
        } else {
            display.classList.remove('is-invalid');
        }
    }

    const observer = new MutationObserver(function () {
        syncValidationState();
    });

    observer.observe(select, {
        attributes: true,
        attributeFilter: ['class']
    });

    syncValidationState();
    updateSelectedState();
}

function closeAllDropdowns() {
    document.querySelectorAll('.custom-select-dropdown').forEach(dropdown => {
        dropdown.classList.remove('show');
    });
}

document.addEventListener('DOMContentLoaded', function () {
    document.addEventListener('click', closeAllDropdowns);
});