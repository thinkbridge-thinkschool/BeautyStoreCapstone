import { Component, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { AdminService } from '../../services/admin.service';
import type { AdminCategory, CreateCategoryRequest, UpdateCategoryRequest } from '../../models/admin.models';

@Component({
  selector: 'app-admin-categories',
  imports: [ReactiveFormsModule],
  templateUrl: './categories.component.html',
  styleUrl: './categories.component.scss',
})
export class CategoriesComponent {
  #admin = inject(AdminService);
  #fb    = inject(FormBuilder);

  editing  = signal<AdminCategory | null>(null);
  creating = signal(false);
  saving   = signal(false);
  deleting = signal<number | null>(null);
  error    = signal<string | null>(null);
  uploadingImage = signal(false);

  categoriesResource = rxResource({ stream: () => this.#admin.getCategories() });

  readonly fallbackImage = 'data:image/svg+xml,' + encodeURIComponent(
    '<svg xmlns="http://www.w3.org/2000/svg" width="40" height="40">' +
    '<rect width="40" height="40" fill="#f3f4f6"/></svg>'
  );

  form = this.#fb.nonNullable.group({
    name:         ['', [Validators.required, Validators.maxLength(100)]],
    slug:         ['', [Validators.required, Validators.maxLength(100), Validators.pattern(/^[a-z0-9-]+$/)]],
    description:  ['' as string | null],
    imageUrl:     ['' as string | null],
    displayOrder: [0,  [Validators.required, Validators.min(0)]],
    isActive:     [true],
  });

  openCreate() {
    this.form.reset({ displayOrder: 0, isActive: true });
    this.editing.set(null);
    this.creating.set(true);
    this.error.set(null);
  }

  openEdit(c: AdminCategory) {
    this.form.setValue({
      name:         c.name,
      slug:         c.slug,
      description:  c.description ?? '',
      imageUrl:     c.imageUrl ?? '',
      displayOrder: c.displayOrder,
      isActive:     c.isActive,
    });
    this.editing.set(c);
    this.creating.set(true);
    this.error.set(null);
  }

  closeModal() {
    this.creating.set(false);
    this.editing.set(null);
  }

  autoSlug() {
    const name = this.form.get('name')?.value ?? '';
    if (!this.editing()) {
      const slug = name.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '');
      this.form.patchValue({ slug });
    }
  }

  save() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving.set(true);
    this.error.set(null);

    const val = this.form.getRawValue();
    const onNext = () => { this.saving.set(false); this.closeModal(); this.categoriesResource.reload(); };
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const onError = (e: any) => {
      this.saving.set(false);
      this.error.set(e?.error?.title ?? 'Failed to save category.');
    };

    const existing = this.editing();
    if (existing) {
      const req: UpdateCategoryRequest = {
        name: val.name, slug: val.slug, description: val.description || null,
        imageUrl: val.imageUrl || null, displayOrder: val.displayOrder, isActive: val.isActive,
      };
      this.#admin.updateCategory(existing.id, req).subscribe({ next: onNext, error: onError });
    } else {
      const req: CreateCategoryRequest = {
        name: val.name, slug: val.slug, description: val.description || null,
        imageUrl: val.imageUrl || null, displayOrder: val.displayOrder,
      };
      this.#admin.createCategory(req).subscribe({ next: onNext, error: onError });
    }
  }

  deleteCategory(c: AdminCategory) {
    const label = c.productCount > 0
      ? `"${c.name}" has ${c.productCount} product(s). It will be hidden but not permanently deleted. Continue?`
      : `Permanently delete "${c.name}"? This cannot be undone.`;
    if (!confirm(label)) return;

    this.deleting.set(c.id);
    this.error.set(null);
    this.#admin.deleteCategory(c.id).subscribe({
      next: () => { this.deleting.set(null); this.categoriesResource.reload(); },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      error: (e: any) => {
        this.deleting.set(null);
        this.error.set(e?.error?.title ?? 'Failed to delete category.');
      },
    });
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file  = input.files?.[0];
    if (!file) return;
    this.uploadingImage.set(true);
    this.#admin.uploadImage(file).subscribe({
      next: (res) => { this.form.patchValue({ imageUrl: res.url }); this.uploadingImage.set(false); },
      error: () => { this.uploadingImage.set(false); this.error.set('Image upload failed.'); },
    });
  }
}
