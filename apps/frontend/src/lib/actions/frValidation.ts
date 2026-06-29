// Svelte action: forces native HTML5 validation bubbles to display in French,
// regardless of the browser's UI language. Apply it to a <form> element:
//
//   <form method="POST" use:frValidation> … </form>
//
// On a control's `invalid` event (captured at the form, since `invalid` does not
// bubble) it sets a French `setCustomValidity()` message matched to the failure
// type, and clears it again on `input`/`change` so the field can re-validate.
// We only ever override the *native* constraint messages — if app code has set
// its own custom message, we leave it alone.

type ValidatableControl = HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement;

function isValidatable(el: EventTarget | null): el is ValidatableControl {
	return (
		el instanceof HTMLInputElement ||
		el instanceof HTMLSelectElement ||
		el instanceof HTMLTextAreaElement
	);
}

// Build the French message for the first failing constraint on a control.
function frMessage(el: ValidatableControl): string {
	const v = el.validity;

	if (v.valueMissing) {
		return el instanceof HTMLSelectElement
			? 'Veuillez sélectionner un élément dans la liste.'
			: 'Veuillez renseigner ce champ.';
	}
	if (v.typeMismatch) {
		if (el instanceof HTMLInputElement && el.type === 'email') {
			return 'Veuillez saisir une adresse e-mail valide.';
		}
		if (el instanceof HTMLInputElement && el.type === 'url') {
			return 'Veuillez saisir une URL valide.';
		}
		return 'Veuillez saisir une valeur valide.';
	}
	if (v.rangeUnderflow && el instanceof HTMLInputElement) {
		return `La valeur doit être supérieure ou égale à ${el.min}.`;
	}
	if (v.rangeOverflow && el instanceof HTMLInputElement) {
		return `La valeur doit être inférieure ou égale à ${el.max}.`;
	}
	if (v.tooShort && el instanceof HTMLInputElement) {
		return `Veuillez saisir au moins ${el.minLength} caractères.`;
	}
	if (v.tooLong && el instanceof HTMLInputElement) {
		return `Veuillez saisir au maximum ${el.maxLength} caractères.`;
	}
	if (v.stepMismatch) {
		return 'Veuillez saisir une valeur valide.';
	}
	if (v.patternMismatch) {
		// A pattern on an email field narrows the native check (e.g. requires a TLD),
		// so surface the email-specific message rather than the generic one.
		if (el instanceof HTMLInputElement && el.type === 'email') {
			return 'Veuillez saisir une adresse e-mail valide.';
		}
		return 'Veuillez respecter le format demandé.';
	}
	if (v.badInput) {
		return 'Veuillez saisir une valeur valide.';
	}
	return 'Champ invalide.';
}

export function frValidation(form: HTMLFormElement) {
	const onInvalid = (e: Event) => {
		const el = e.target;
		if (!isValidatable(el)) return;
		// Don't clobber a message the app set deliberately via setCustomValidity.
		if (el.validity.customError) return;
		el.setCustomValidity(frMessage(el));
	};

	// Clear our override as soon as the user edits, so the control re-evaluates
	// its real validity on the next submit (a stale customValidity stays invalid).
	const onInput = (e: Event) => {
		const el = e.target;
		if (isValidatable(el)) el.setCustomValidity('');
	};

	// `invalid` does not bubble — capture it at the form. `input`/`change` bubble.
	form.addEventListener('invalid', onInvalid, true);
	form.addEventListener('input', onInput);
	form.addEventListener('change', onInput);

	return {
		destroy() {
			form.removeEventListener('invalid', onInvalid, true);
			form.removeEventListener('input', onInput);
			form.removeEventListener('change', onInput);
		}
	};
}
