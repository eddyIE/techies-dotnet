// Spec rows and gallery images are edited client-side; on submit the remaining rows are
// renumbered into Specs[i].* / ExistingImages[i] so the model binder sees a dense list.
(() => {
    const form = document.getElementById("product-form");
    const specRows = document.getElementById("spec-rows");
    const gallery = document.getElementById("gallery");

    form.querySelector("[data-add-spec]").addEventListener("click", () => {
        specRows.append(document.getElementById("spec-template").content.cloneNode(true));
        specRows.lastElementChild.querySelector("input").focus();
    });

    form.addEventListener("click", (e) => {
        const button = e.target.closest("button");
        if (!button) return;
        const row = button.closest("[data-spec], [data-image]");
        if (button.hasAttribute("data-remove")) row.remove();
        if (button.hasAttribute("data-up") && row.previousElementSibling) row.after(row.previousElementSibling);
        if (button.hasAttribute("data-down") && row.nextElementSibling) row.before(row.nextElementSibling);
    });

    form.addEventListener("submit", () => {
        specRows.querySelectorAll("[data-spec]").forEach((row, i) =>
            row.querySelectorAll("[data-field]").forEach((input) => (input.name = `Specs[${i}].${input.dataset.field}`)));
        gallery.querySelectorAll("[data-image] [data-field=url]").forEach((input, i) => (input.name = `ExistingImages[${i}]`));
    });
})();
