(() => {
    const shelf = document.getElementById('shelf');
    const wrap = document.getElementById('books');
    if (!shelf || !wrap) return;   // страница без рафт

    const capLabel = document.getElementById('capLabel');
    const capT = document.getElementById('capTitle');
    const capA = document.getElementById('capAuthor');
    const btnPrev = document.getElementById('prev');
    const btnNext = document.getElementById('next');
    const btnInfo = document.getElementById('info');
    const infoPanel = document.getElementById('recInfo');
    const infoT = document.getElementById('recInfoTitle');
    const infoA = document.getElementById('recInfoAuthor');
    const infoD = document.getElementById('recInfoText');

    // цветове за корицата (c) и текста (f) – вземат се по ред
    const PALETTE = [
        { c: '#F1A8B2', f: '#472950' }, { c: '#DDC3E0', f: '#472950' },
        { c: '#78201B', f: '#FFEBE1' }, { c: '#472950', f: '#FFEBE1' },
        { c: '#FFEBE1', f: '#78201B' }, { c: '#A5527A', f: '#FFEBE1' },
        { c: '#8E5A9A', f: '#472950' }, { c: '#2E1A36', f: '#DDC3E0' }
    ];

    const clamp = (v, a, b) => Math.min(b, Math.max(a, v));
    const GAP = 6;
    let SPINE_W = 34, COVER_W = 200;
    let items = [], books = [], label = '';
    let target = 0, pos = 0, running = false, lastShown = -1, infoOpen = false;

    function setCaption(l, t, a) {
        if (capLabel) capLabel.textContent = l;
        if (capT) capT.textContent = t;
        if (capA) capA.textContent = a;
    }

    // Текстът идва от ИИ -> слагаме го с textContent (не innerHTML), за да няма XSS
    function buildBooks() {
        books = items.map(it => {
            const b = document.createElement('div');
            b.className = 'book';

            const spine = document.createElement('div');
            spine.className = 'spine';
            spine.textContent = it.t;

            const cover = document.createElement('div');
            cover.className = 'cover';
            const ct = document.createElement('div');
            ct.className = 'ct';
            ct.textContent = it.t;
            const ca = document.createElement('div');
            ca.className = 'ca';
            ca.textContent = it.a;
            cover.append(ct, ca);

            [spine, cover].forEach(el => { el.style.background = it.c; el.style.color = it.f; });
            b.append(spine, cover);
            wrap.appendChild(b);
            return b;
        });
    }

    function size() {
        COVER_W = clamp(wrap.clientWidth * 0.36, 140, 220);
        books.forEach(b => b.style.setProperty('--cover-w', COVER_W + 'px'));
    }

    function fillInfo() {
        const it = items[lastShown];
        if (!it) return;
        infoT.textContent = it.t;
        infoA.textContent = it.a;
        infoD.textContent = it.d || 'Няма налично описание.';
    }

    function draw() {
        if (!books.length) return;
        const last = books.length - 1;
        const ts = books.map((_, i) => clamp(1 - Math.abs(i - pos), 0, 1));
        const widths = ts.map(t => SPINE_W + (COVER_W - SPINE_W) * t);
        let x = 0;
        const lefts = widths.map(w => { const l = x; x += w + GAP; return l; });
        const i0 = Math.floor(pos), i1 = Math.min(i0 + 1, last), f = pos - i0;
        const c0 = lefts[i0] + widths[i0] / 2, c1 = lefts[i1] + widths[i1] / 2;
        const offset = wrap.clientWidth / 2 - (c0 + (c1 - c0) * f);

        books.forEach((b, i) => {
            const t = ts[i], h = 78 + 22 * t + (i % 3) * 3 * (1 - t);   // лека разлика във височините
            b.style.width = widths[i] + 'px';
            b.style.height = h + '%';
            b.style.transform = `translateX(${lefts[i] + offset}px)`;
            b.style.zIndex = Math.round(t * 10);
            b.children[1].style.transform = `rotateY(${-90 * (1 - t)}deg)`;   // корица
            b.children[0].style.opacity = 1 - t;                              // гръб
        });

        const cur = Math.round(pos);
        if (cur !== lastShown) {
            lastShown = cur;
            setCaption(label, items[cur].t, items[cur].a);
            if (infoOpen) fillInfo();   // ако описанието е отворено, то следва избраната книга
        }
    }

    function loop() {
        pos += (target - pos) * 0.12;
        if (Math.abs(target - pos) < 0.001) { pos = target; running = false; }
        try {
            draw();
        } finally {
            if (running) requestAnimationFrame(loop);
        }
    }
    function kick() { if (!running) { running = true; requestAnimationFrame(loop); } }
    function set(v) {
        if (!books.length) return;
        target = clamp(v, 0, books.length - 1);
        kick();
    }

    // ---------- управление ----------
    if (btnNext) btnNext.onclick = () => set(Math.round(target) + 1);
    if (btnPrev) btnPrev.onclick = () => set(Math.round(target) - 1);

    if (btnInfo) btnInfo.onclick = () => {
        infoOpen = !infoOpen;
        infoPanel.hidden = !infoOpen;
        btnInfo.setAttribute('aria-expanded', String(infoOpen));
        btnInfo.textContent = infoOpen ? 'Скрий информацията' : 'Повече информация';
        if (infoOpen) fillInfo();
    };

    shelf.addEventListener('wheel', e => {
        if (Math.abs(e.deltaX) > Math.abs(e.deltaY) || e.shiftKey) { e.preventDefault(); set(target + (e.deltaX || e.deltaY) / 300); }
    }, { passive: false });
    shelf.addEventListener('keydown', e => {
        if (e.key === 'ArrowRight') set(Math.round(target) + 1);
        if (e.key === 'ArrowLeft') set(Math.round(target) - 1);
    });
    shelf.tabIndex = 0;

    let startX = null, startT = 0, moved = false;
    shelf.addEventListener('pointerdown', e => { startX = e.clientX; startT = target; moved = false; });
    window.addEventListener('pointermove', e => {
        if (startX === null) return;
        if (Math.abs(e.clientX - startX) > 5) { moved = true; shelf.classList.add('dragging'); }
        if (moved) set(startT - (e.clientX - startX) / 110);
    });
    window.addEventListener('pointerup', () => {
        if (startX === null) return;
        startX = null; shelf.classList.remove('dragging');
        if (moved) set(Math.round(target));
    });
    shelf.addEventListener('click', e => {
        if (moved) return;
        const i = books.indexOf(e.target.closest('.book'));
        if (i >= 0) set(i);
    });
    window.addEventListener('resize', () => { size(); draw(); });

    // ---------- зареждане на препоръките от /Home/Recommendations ----------
    async function load() {
        setCaption('', 'Зареждам препоръките…', '');
        try {
            const res = await fetch(shelf.dataset.url, {
                headers: { Accept: 'application/json' },
                credentials: 'same-origin'
            });
            if (!res.ok) throw new Error('HTTP ' + res.status);

            const data = await res.json();
            if (!Array.isArray(data.items) || data.items.length === 0) throw new Error('Няма препоръки');

            items = data.items.map((r, i) => ({
                t: r.title, a: r.author, d: r.description,
                ...PALETTE[i % PALETTE.length]
            }));
            label = data.personalized ? 'Препоръки за теб' : 'Популярни сега';

            buildBooks();
            target = pos = Math.floor((items.length - 1) / 2);   // започваме от средата
            if (btnInfo) btnInfo.disabled = false;
            size();
            draw();
        } catch (err) {
            console.warn('Препоръките не се заредиха:', err);
            setCaption('', 'Препоръките не са налични в момента.', 'Опитай отново след малко.');
        }
    }

    load();
})();