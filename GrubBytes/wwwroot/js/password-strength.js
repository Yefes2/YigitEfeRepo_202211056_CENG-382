function initPasswordStrength(inputId, barId, labelId) {
    const input = document.getElementById(inputId);
    const bar = document.getElementById(barId);
    const label = document.getElementById(labelId);

    if (!input || !bar || !label) return;

    const levels = [
        { label: 'Too short', color: '#888880', width: '0%' },
        { label: 'Weak', color: '#e74c3c', width: '25%' },
        { label: 'Fair', color: '#FFB347', width: '50%' },
        { label: 'Good', color: '#f1c40f', width: '75%' },
        { label: 'Strong', color: '#2ECC71', width: '100%' },
    ];

    function score(pw) {
        if (pw.length < 6) return 0;
        let s = 1;
        if (pw.length >= 10) s++;
        if (/[A-Z]/.test(pw)) s++;
        if (/[0-9]/.test(pw)) s++;
        if (/[^A-Za-z0-9]/.test(pw)) s++;
        return Math.min(s, 4);
    }

    input.addEventListener('input', () => {
        const s = score(input.value);
        const l = levels[s];
        bar.style.width = input.value.length === 0 ? '0%' : l.width;
        bar.style.background = l.color;
        label.textContent = input.value.length === 0 ? '' : l.label;
        label.style.color = l.color;
    });
}