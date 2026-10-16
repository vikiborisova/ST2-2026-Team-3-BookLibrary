document.addEventListener('DOMContentLoaded', () => {
    const card = document.getElementById('flipCard');
    if (!card) return;

    const front = card.querySelector('.auth-front');
    const back = card.querySelector('.auth-back');

    // hide the pages
    function sync() {
        const flipped = card.classList.contains('is-flipped');
        front.inert = flipped;
        back.inert = !flipped;
    }

    // flip the card without page reloud
    document.querySelectorAll('[data-flip]').forEach(link => {
        link.addEventListener('click', e => {
            e.preventDefault();
            card.classList.toggle('is-flipped');
            sync();
            history.pushState(null, '', link.href);
            document.title = link.dataset.title + ' - BookLibrary';
        });
    });

    // napred/nazad buttons
    window.addEventListener('popstate', () => {
        const onRegister = location.pathname.toLowerCase().includes('/register');
        card.classList.toggle('is-flipped', onRegister);
        sync();
    });

    sync();
});
