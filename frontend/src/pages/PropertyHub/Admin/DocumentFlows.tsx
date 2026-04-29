import React, { useCallback, useEffect, useMemo, useState } from 'react';
import {
  DocumentExtractionTemplateDto,
  DocumentLabelSetDto,
  DocumentSummarisationTemplateDto,
  DocumentWorkflowRuleDto,
  DocumentWorkflowRuleTestResponse,
  UpsertDocumentWorkflowStepRequest,
  documentHubService,
} from '../../../services/documentHubService';
import {
  propertyAdminService,
  type ContactLogTypeDto,
  type JournalTypeDto,
  type PropertyGroupResponseDto,
  type PropertyResponseDto,
  type TenantResponseDto,
} from '../../../services/propertyAdminService';

type EditableWorkflowStep = UpsertDocumentWorkflowStepRequest;

const parseStepConfig = (raw?: string): Record<string, string | number | boolean> => {
  if (!raw?.trim()) return {};
  try {
    const parsed = JSON.parse(raw);
    return typeof parsed === 'object' && parsed !== null ? (parsed as Record<string, string | number | boolean>) : {};
  } catch {
    return {};
  }
};

const stringifyStepConfig = (config: Record<string, string | number | boolean>): string => {
  const filtered = Object.fromEntries(
    Object.entries(config).filter(([, value]) => value !== '' && value !== null && value !== undefined)
  );
  return Object.keys(filtered).length === 0 ? '' : JSON.stringify(filtered);
};

const DocumentFlows: React.FC = () => {
  const [loading, setLoading] = useState(false);
  const [feedback, setFeedback] = useState<string | null>(null);
  const [labelSets, setLabelSets] = useState<DocumentLabelSetDto[]>([]);
  const [extractionTemplates, setExtractionTemplates] = useState<DocumentExtractionTemplateDto[]>([]);
  const [summarisationTemplates, setSummarisationTemplates] = useState<DocumentSummarisationTemplateDto[]>([]);
  const [workflowRules, setWorkflowRules] = useState<DocumentWorkflowRuleDto[]>([]);

  // Lookup datasets for user-friendly dropdowns in workflow steps.
  const [propertyGroups, setPropertyGroups] = useState<PropertyGroupResponseDto[]>([]);
  const [properties, setProperties] = useState<PropertyResponseDto[]>([]);
  const [tenants, setTenants] = useState<TenantResponseDto[]>([]);
  const [journalTypes, setJournalTypes] = useState<JournalTypeDto[]>([]);
  const [contactLogTypes, setContactLogTypes] = useState<ContactLogTypeDto[]>([]);

  const [editingWorkflowRuleId, setEditingWorkflowRuleId] = useState<number | null>(null);
  const [workflowName, setWorkflowName] = useState('');
  const [workflowClassificationLabel, setWorkflowClassificationLabel] = useState('');
  const [workflowMinimumScore, setWorkflowMinimumScore] = useState(0.28);
  const [workflowPriority, setWorkflowPriority] = useState(100);
  const [workflowStopOnFailure, setWorkflowStopOnFailure] = useState(true);
  const [workflowSteps, setWorkflowSteps] = useState<EditableWorkflowStep[]>([
    { stepOrder: 1, stepType: 'SetCategory', stepConfigJson: '', isActive: true },
  ]);

  const [workflowTestRuleId, setWorkflowTestRuleId] = useState<number | null>(null);
  const [workflowTestFile, setWorkflowTestFile] = useState<File | null>(null);
  const [workflowTestResult, setWorkflowTestResult] = useState<DocumentWorkflowRuleTestResponse | null>(null);
  const [workflowTestFeedback, setWorkflowTestFeedback] = useState<string | null>(null);

  const getFriendlyError = useCallback((error: unknown): string => {
    const raw = error instanceof Error ? error.message : 'Unexpected error.';
    return raw.toLowerCase().includes('404')
      ? 'Workflow API is unavailable on the backend deployment.'
      : raw;
  }, []);

  const availableClassificationLabels = useMemo(() => {
    const labels = labelSets
      .flatMap((set) => set.labels ?? [])
      .map((label) => (label.classificationLabel ?? '').trim())
      .filter((label) => label.length > 0);
    return Array.from(new Set(labels)).sort((a, b) => a.localeCompare(b));
  }, [labelSets]);

  const loadData = useCallback(async () => {
    setLoading(true);
    try {
      const [
        loadedRules,
        loadedLabelSets,
        loadedExtractionTemplates,
        loadedSummarisationTemplates,
        loadedPropertyGroups,
        loadedProperties,
        loadedTenants,
        loadedJournalTypes,
        loadedContactLogTypes,
      ] = await Promise.all([
        documentHubService.getWorkflowRules(),
        documentHubService.getLabelSets(),
        documentHubService.getExtractionTemplates(),
        documentHubService.getSummarisationTemplates(),
        propertyAdminService.getPropertyGroups(),
        propertyAdminService.getProperties(),
        propertyAdminService.getTenants(),
        propertyAdminService.getJournalTypes(),
        propertyAdminService.getContactLogTypes(),
      ]);
      setWorkflowRules(loadedRules);
      setLabelSets(loadedLabelSets);
      setExtractionTemplates(loadedExtractionTemplates);
      setSummarisationTemplates(loadedSummarisationTemplates);
      setPropertyGroups(loadedPropertyGroups);
      setProperties(loadedProperties);
      setTenants(loadedTenants);
      setJournalTypes(loadedJournalTypes);
      setContactLogTypes(loadedContactLogTypes);
    } catch (error) {
      setFeedback(getFriendlyError(error));
    } finally {
      setLoading(false);
    }
  }, [getFriendlyError]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const resetWorkflowForm = () => {
    setEditingWorkflowRuleId(null);
    setWorkflowName('');
    setWorkflowClassificationLabel('');
    setWorkflowMinimumScore(0.28);
    setWorkflowPriority(100);
    setWorkflowStopOnFailure(true);
    setWorkflowSteps([{ stepOrder: 1, stepType: 'SetCategory', stepConfigJson: '', isActive: true }]);
  };

  const handleAddWorkflowStep = () => {
    setWorkflowSteps((prev) => [
      ...prev,
      { stepOrder: prev.length + 1, stepType: 'MoveToFolder', stepConfigJson: '', isActive: true },
    ]);
  };

  const handleUpdateWorkflowStep = (idx: number, patch: Partial<EditableWorkflowStep>) => {
    setWorkflowSteps((prev) => prev.map((s, i) => (i === idx ? { ...s, ...patch, stepOrder: i + 1 } : s)));
  };

  const handleRemoveWorkflowStep = (idx: number) => {
    setWorkflowSteps((prev) => prev.filter((_, i) => i !== idx).map((s, i) => ({ ...s, stepOrder: i + 1 })));
  };

  const handleUpdateWorkflowStepConfigField = (idx: number, key: string, value: string | number | boolean) => {
    setWorkflowSteps((prev) =>
      prev.map((step, index) => {
        if (index !== idx) return step;
        const config = parseStepConfig(step.stepConfigJson);
        config[key] = value;
        return { ...step, stepOrder: index + 1, stepConfigJson: stringifyStepConfig(config) };
      })
    );
  };

  const handleUpdateWorkflowStepConfigFields = (
    idx: number,
    patch: Record<string, string | number | boolean>
  ) => {
    setWorkflowSteps((prev) =>
      prev.map((step, index) => {
        if (index !== idx) return step;
        const config = parseStepConfig(step.stepConfigJson);
        Object.entries(patch).forEach(([key, value]) => {
          config[key] = value;
        });
        return { ...step, stepOrder: index + 1, stepConfigJson: stringifyStepConfig(config) };
      })
    );
  };

  const handleEditWorkflowRule = (rule: DocumentWorkflowRuleDto) => {
    setEditingWorkflowRuleId(rule.documentWorkflowRuleId);
    setWorkflowName(rule.workflowName);
    setWorkflowClassificationLabel(rule.classificationLabel);
    setWorkflowMinimumScore(rule.minimumScore);
    setWorkflowPriority(rule.priority);
    setWorkflowStopOnFailure(rule.stopOnFailure);
    setWorkflowSteps(
      (rule.steps ?? []).length
        ? rule.steps.map((s, idx) => ({
            stepOrder: idx + 1,
            stepType: s.stepType,
            stepConfigJson: s.stepConfigJson ?? '',
            isActive: s.isActive,
          }))
        : [{ stepOrder: 1, stepType: 'SetCategory', stepConfigJson: '', isActive: true }]
    );
  };

  const handleSaveWorkflowRule = async () => {
    if (!workflowName.trim() || !workflowClassificationLabel.trim()) {
      setFeedback('Workflow name and classification label are required.');
      return;
    }
    const steps = workflowSteps
      .map((s, idx) => ({ ...s, stepOrder: idx + 1, stepType: s.stepType.trim(), stepConfigJson: s.stepConfigJson?.trim() || undefined }))
      .filter((s) => s.stepType);
    if (steps.length === 0) {
      setFeedback('At least one step is required.');
      return;
    }

    setLoading(true);
    setFeedback(null);
    try {
      if (editingWorkflowRuleId) {
        await documentHubService.updateWorkflowRule(editingWorkflowRuleId, {
          workflowName: workflowName.trim(),
          classificationLabel: workflowClassificationLabel.trim(),
          minimumScore: workflowMinimumScore,
          priority: workflowPriority,
          stopOnFailure: workflowStopOnFailure,
          steps,
        });
      } else {
        await documentHubService.createWorkflowRule({
          workflowName: workflowName.trim(),
          classificationLabel: workflowClassificationLabel.trim(),
          minimumScore: workflowMinimumScore,
          priority: workflowPriority,
          stopOnFailure: workflowStopOnFailure,
          steps,
        });
      }
      resetWorkflowForm();
      setFeedback('Workflow rule saved.');
      await loadData();
    } catch (error) {
      setFeedback(getFriendlyError(error));
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteWorkflowRule = async (ruleId: number, name: string) => {
    if (!window.confirm(`Delete workflow rule "${name}"?`)) return;
    setLoading(true);
    try {
      await documentHubService.deleteWorkflowRule(ruleId);
      if (editingWorkflowRuleId === ruleId) resetWorkflowForm();
      await loadData();
    } catch (error) {
      setFeedback(getFriendlyError(error));
    } finally {
      setLoading(false);
    }
  };

  const handleRunWorkflowTest = async () => {
    if (!workflowTestRuleId || !workflowTestFile) {
      setWorkflowTestFeedback('Select a rule and upload a test document.');
      return;
    }
    setLoading(true);
    setWorkflowTestFeedback(null);
    try {
      const result = await documentHubService.testWorkflowRule(workflowTestRuleId, workflowTestFile);
      setWorkflowTestResult(result);
      setWorkflowTestFeedback(`Test completed for "${result.workflowName}".`);
    } catch (error) {
      setWorkflowTestFeedback(getFriendlyError(error));
      setWorkflowTestResult(null);
    } finally {
      setLoading(false);
    }
  };

  const normalizeIdForSelect = (value: string | number | boolean | undefined): string => {
    if (value === undefined || value === null) return '';
    const rawNum = typeof value === 'number' ? value : Number(value);
    if (!Number.isFinite(rawNum) || rawNum <= 0) return '';
    return String(rawNum);
  };

  return (
    <div className="space-y-6">
      <div className="rounded-xl border border-slate-200 bg-white p-5">
        <h2 className="text-2xl font-semibold tracking-tight text-slate-900">Document Flows</h2>
        <p className="mt-1 text-sm text-slate-500">Configure post-classification workflow actions and test them safely.</p>
        <p className="mt-2 text-xs text-slate-500">
          Current flow steps support category/folder actions, completion, journal/contact logs, and entity extraction.
          Text summarisation is now available via <span className="font-semibold">RunSummarisation</span>.
        </p>
        {feedback && <p className="mt-3 rounded-lg border border-slate-200 bg-slate-50 px-3 py-2 text-xs text-slate-700">{feedback}</p>}
      </div>

      <div className="rounded-xl border border-slate-200 bg-white p-5 text-xs text-slate-700">
        <div className="grid gap-2">
          <input value={workflowName} onChange={(e) => setWorkflowName(e.target.value)} className="w-full rounded border border-slate-300 px-2 py-1" placeholder="Workflow name" />
          <input list="workflow-classification-label-options" value={workflowClassificationLabel} onChange={(e) => setWorkflowClassificationLabel(e.target.value)} className="w-full rounded border border-slate-300 px-2 py-1" placeholder="Classification label" />
          <datalist id="workflow-classification-label-options">
            {availableClassificationLabels.map((label) => <option key={label} value={label} />)}
            <option value="Unclassified" />
          </datalist>
        </div>
        <div className="mt-2 grid grid-cols-2 gap-2">
          <input type="number" min={0} max={1} step={0.01} value={workflowMinimumScore} onChange={(e) => setWorkflowMinimumScore(Number(e.target.value || 0.28))} className="rounded border border-slate-300 px-2 py-1" />
          <input type="number" min={1} value={workflowPriority} onChange={(e) => setWorkflowPriority(Number(e.target.value || 100))} className="rounded border border-slate-300 px-2 py-1" />
        </div>
        <label className="mt-2 inline-flex items-center gap-2"><input type="checkbox" checked={workflowStopOnFailure} onChange={(e) => setWorkflowStopOnFailure(e.target.checked)} />Stop on failure</label>

        <div className="mt-3 space-y-2 rounded border border-slate-200 bg-slate-50 p-2">
          {workflowSteps.map((step, idx) => (
            <div key={idx} className="rounded border border-slate-200 bg-white p-2">
              <div className="grid gap-2 md:grid-cols-[90px_1fr_auto]">
                <span className="self-center">Step {idx + 1}</span>
                <select value={step.stepType} onChange={(e) => handleUpdateWorkflowStep(idx, { stepType: e.target.value })} className="rounded border border-slate-300 px-2 py-1">
                  <option value="SetCategory">SetCategory</option>
                  <option value="MoveToFolder">MoveToFolder</option>
                  <option value="MarkCompleted">MarkCompleted</option>
                  <option value="CreateJournalLog">CreateJournalLog</option>
                  <option value="CreateContactLog">CreateContactLog</option>
                  <option value="RunExtraction">RunExtraction</option>
                  <option value="RunSummarisation">RunSummarisation</option>
                </select>
                <button type="button" onClick={() => handleRemoveWorkflowStep(idx)} className="rounded border border-rose-200 bg-rose-50 px-2 py-1 text-rose-700">Remove</button>
              </div>
              <input value={step.stepConfigJson ?? ''} onChange={(e) => handleUpdateWorkflowStep(idx, { stepConfigJson: e.target.value })} className="mt-2 w-full rounded border border-slate-300 px-2 py-1" placeholder="Optional JSON config" />
              {step.stepType === 'SetCategory' && (
                <input
                  value={String(parseStepConfig(step.stepConfigJson).category ?? '')}
                  onChange={(e) => handleUpdateWorkflowStepConfigField(idx, 'category', e.target.value)}
                  className="mt-2 w-full rounded border border-slate-300 px-2 py-1"
                  placeholder='Optional override category (blank = classification label)'
                />
              )}
              {step.stepType === 'MoveToFolder' && (
                <input value={String(parseStepConfig(step.stepConfigJson).destinationPath ?? '')} onChange={(e) => handleUpdateWorkflowStepConfigField(idx, 'destinationPath', e.target.value)} className="mt-2 w-full rounded border border-slate-300 px-2 py-1" placeholder='destinationPath e.g. "Inbox/Property Hub/Citiq"' />
              )}
              {step.stepType === 'CreateJournalLog' && (
                <div className="mt-2 grid gap-1 md:grid-cols-2">
                  {(() => {
                    const config = parseStepConfig(step.stepConfigJson);
                    const selectedJournalTypeId = Number(config.journalTypeId ?? 0);
                    const selectedPropertyGroupId = Number(config.propertyGroupId ?? 0);

                    const filteredProperties = selectedPropertyGroupId
                      ? properties.filter((p) => p.propertyGroupId === selectedPropertyGroupId)
                      : properties;

                    const selectedJournalType = journalTypes.find((t) => t.journalTypeId === selectedJournalTypeId);
                    const subTypes = selectedJournalType?.subTypes ?? [];

                    return (
                      <>
                        <div className="md:col-span-2">
                          <label className="block text-[11px] font-medium text-slate-600">Journal Type</label>
                          <select
                            value={normalizeIdForSelect(config.journalTypeId)}
                            onChange={(e) => {
                              const v = e.target.value;
                              handleUpdateWorkflowStepConfigFields(idx, {
                                journalTypeId: v === '' ? '' : Number(v),
                                journalSubTypeId: '',
                              });
                            }}
                            className="mt-1 w-full rounded border border-slate-300 px-2 py-1"
                          >
                            <option value="">Select journal type</option>
                            {journalTypes.map((t) => (
                              <option key={t.journalTypeId} value={t.journalTypeId}>
                                {t.journalTypeName}
                              </option>
                            ))}
                          </select>
                        </div>

                        <div className="md:col-span-2">
                          <label className="block text-[11px] font-medium text-slate-600">Journal Sub Type</label>
                          <select
                            value={normalizeIdForSelect(config.journalSubTypeId)}
                            onChange={(e) =>
                              handleUpdateWorkflowStepConfigField(idx, 'journalSubTypeId', e.target.value === '' ? '' : Number(e.target.value))
                            }
                            className="mt-1 w-full rounded border border-slate-300 px-2 py-1"
                            disabled={!selectedJournalTypeId}
                          >
                            <option value="">{selectedJournalTypeId ? 'Select sub type' : 'Select journal type first'}</option>
                            {subTypes.map((st) => (
                              <option key={st.journalSubTypeId} value={st.journalSubTypeId}>
                                {st.journalSubTypeName}
                              </option>
                            ))}
                          </select>
                        </div>

                        <div>
                          <label className="block text-[11px] font-medium text-slate-600">Property Group</label>
                          <select
                            value={normalizeIdForSelect(config.propertyGroupId)}
                            onChange={(e) => {
                              const v = e.target.value;
                              handleUpdateWorkflowStepConfigFields(idx, {
                                propertyGroupId: v === '' ? '' : Number(v),
                                propertyId: '',
                              });
                            }}
                            className="mt-1 w-full rounded border border-slate-300 px-2 py-1"
                          >
                            <option value="">Select property group</option>
                            {propertyGroups.map((g) => (
                              <option key={g.propertyGroupId} value={g.propertyGroupId}>
                                {g.propertyGroupName}
                              </option>
                            ))}
                          </select>
                        </div>

                        <div>
                          <label className="block text-[11px] font-medium text-slate-600">Property</label>
                          <select
                            value={normalizeIdForSelect(config.propertyId)}
                            onChange={(e) => {
                              const v = e.target.value;
                              const prop = properties.find((p) => p.propertyId === Number(v));
                              handleUpdateWorkflowStepConfigFields(idx, {
                                propertyId: v === '' ? '' : Number(v),
                                propertyGroupId: v === '' ? '' : prop?.propertyGroupId ?? '',
                              });
                            }}
                            className="mt-1 w-full rounded border border-slate-300 px-2 py-1"
                          >
                            <option value="">Select property</option>
                            {filteredProperties.map((p) => (
                              <option key={p.propertyId} value={p.propertyId}>
                                {p.propertyName}
                              </option>
                            ))}
                          </select>
                        </div>

                        <div className="md:col-span-2">
                          <label className="block text-[11px] font-medium text-slate-600">Tenant</label>
                          <select
                            value={normalizeIdForSelect(config.tenantId)}
                            onChange={(e) => handleUpdateWorkflowStepConfigField(idx, 'tenantId', e.target.value === '' ? '' : Number(e.target.value))}
                            className="mt-1 w-full rounded border border-slate-300 px-2 py-1"
                          >
                            <option value="">Select tenant</option>
                            {tenants.map((t) => (
                              <option key={t.tenantId} value={t.tenantId}>
                                {t.firstName} {t.lastName}
                              </option>
                            ))}
                          </select>
                          <p className="mt-1 text-[10px] text-slate-500">
                            These selections are written as integer IDs for `tblJournalLog` (journalTypeId, journalSubTypeId, propertyId, tenantId).
                          </p>
                        </div>

                        <input
                          value={String(config.descriptionTemplate ?? '')}
                          onChange={(e) => handleUpdateWorkflowStepConfigField(idx, 'descriptionTemplate', e.target.value)}
                          className="md:col-span-2 rounded border border-slate-300 px-2 py-1"
                          placeholder="descriptionTemplate (supports {field:<name>}, {classificationLabel}, {classificationScore}, {summary})"
                        />
                        <p className="md:col-span-2 text-[10px] text-slate-500">
                          Token helpers: {'{field:meter_number}'}, {'{field:account_number}'}, {'{field:invoice_total}'},{' '}
                          {'{extractionJson}'}, {'{classificationLabel}'}, {'{classificationScore}'}, {'{summary}'}.
                        </p>
                      </>
                    );
                  })()}
                </div>
              )}
              {step.stepType === 'CreateContactLog' && (
                <div className="mt-2 grid gap-1 md:grid-cols-2">
                  {(() => {
                    const config = parseStepConfig(step.stepConfigJson);
                    const selectedPropertyGroupId = Number(config.propertyGroupId ?? 0);
                    const filteredProperties = selectedPropertyGroupId
                      ? properties.filter((p) => p.propertyGroupId === selectedPropertyGroupId)
                      : properties;

                    return (
                      <>
                        <div className="md:col-span-2">
                          <label className="block text-[11px] font-medium text-slate-600">Contact Log Type</label>
                          <select
                            value={normalizeIdForSelect(config.contactLogTypeId)}
                            onChange={(e) =>
                              handleUpdateWorkflowStepConfigField(
                                idx,
                                'contactLogTypeId',
                                e.target.value === '' ? '' : Number(e.target.value)
                              )
                            }
                            className="mt-1 w-full rounded border border-slate-300 px-2 py-1"
                          >
                            <option value="">Select contact log type</option>
                            {contactLogTypes.map((t) => (
                              <option key={t.contactLogTypeId} value={t.contactLogTypeId}>
                                {t.contactLogTypeName}
                              </option>
                            ))}
                          </select>
                        </div>

                        <div>
                          <label className="block text-[11px] font-medium text-slate-600">Property Group</label>
                          <select
                            value={normalizeIdForSelect(config.propertyGroupId)}
                            onChange={(e) => {
                              const v = e.target.value;
                              handleUpdateWorkflowStepConfigFields(idx, {
                                propertyGroupId: v === '' ? '' : Number(v),
                                propertyId: '',
                              });
                            }}
                            className="mt-1 w-full rounded border border-slate-300 px-2 py-1"
                          >
                            <option value="">Select property group</option>
                            {propertyGroups.map((g) => (
                              <option key={g.propertyGroupId} value={g.propertyGroupId}>
                                {g.propertyGroupName}
                              </option>
                            ))}
                          </select>
                        </div>

                        <div>
                          <label className="block text-[11px] font-medium text-slate-600">Property</label>
                          <select
                            value={normalizeIdForSelect(config.propertyId)}
                            onChange={(e) => {
                              const v = e.target.value;
                              const prop = properties.find((p) => p.propertyId === Number(v));
                              handleUpdateWorkflowStepConfigFields(idx, {
                                propertyId: v === '' ? '' : Number(v),
                                propertyGroupId: v === '' ? '' : prop?.propertyGroupId ?? '',
                              });
                            }}
                            className="mt-1 w-full rounded border border-slate-300 px-2 py-1"
                          >
                            <option value="">Select property</option>
                            {filteredProperties.map((p) => (
                              <option key={p.propertyId} value={p.propertyId}>
                                {p.propertyName}
                              </option>
                            ))}
                          </select>
                        </div>

                        <div className="md:col-span-2">
                          <label className="block text-[11px] font-medium text-slate-600">Tenant</label>
                          <select
                            value={normalizeIdForSelect(config.tenantId)}
                            onChange={(e) => handleUpdateWorkflowStepConfigField(idx, 'tenantId', e.target.value === '' ? '' : Number(e.target.value))}
                            className="mt-1 w-full rounded border border-slate-300 px-2 py-1"
                          >
                            <option value="">Select tenant</option>
                            {tenants.map((t) => (
                              <option key={t.tenantId} value={t.tenantId}>
                                {t.firstName} {t.lastName}
                              </option>
                            ))}
                          </select>
                          <p className="mt-1 text-[10px] text-slate-500">
                            These selections are written as integer IDs for `tblContactLog` (contactLogTypeId, propertyId, propertyGroupId, tenantId).
                          </p>
                        </div>

                        <input
                          value={String(config.contactBy ?? '')}
                          onChange={(e) => handleUpdateWorkflowStepConfigField(idx, 'contactBy', e.target.value)}
                          className="rounded border border-slate-300 px-2 py-1"
                          placeholder="contactBy (optional, default: Workflow)"
                        />

                        <input
                          value={String(config.notesTemplate ?? '')}
                          onChange={(e) => handleUpdateWorkflowStepConfigField(idx, 'notesTemplate', e.target.value)}
                          className="md:col-span-2 rounded border border-slate-300 px-2 py-1"
                          placeholder="notesTemplate (supports {field:<name>}, {classificationLabel}, {classificationScore}, {summary})"
                        />
                        <p className="md:col-span-2 text-[10px] text-slate-500">
                          Token helpers: {'{field:meter_number}'}, {'{field:account_number}'}, {'{field:invoice_total}'},{' '}
                          {'{extractionJson}'}, {'{classificationLabel}'}, {'{classificationScore}'}, {'{summary}'}.
                        </p>
                      </>
                    );
                  })()}
                </div>
              )}
              {step.stepType === 'RunExtraction' && (
                <div className="mt-2 grid gap-1">
                  <select
                    value={String(parseStepConfig(step.stepConfigJson).extractionTemplateId ?? '')}
                    onChange={(e) => handleUpdateWorkflowStepConfigField(idx, 'extractionTemplateId', Number(e.target.value || 0))}
                    className="rounded border border-slate-300 px-2 py-1"
                  >
                    <option value="">Select extraction template</option>
                    {extractionTemplates.map((template) => (
                      <option key={template.documentExtractionTemplateId} value={template.documentExtractionTemplateId}>
                        {template.extractionTemplateName}
                      </option>
                    ))}
                  </select>
                  <p className="text-[11px] text-slate-500">
                    This runs entity extraction and makes fields available to later steps as {`{field:<field_name>}`}.
                  </p>
                </div>
              )}
              {step.stepType === 'RunSummarisation' && (
                <div className="mt-2 grid gap-1">
                  <select
                    value={String(parseStepConfig(step.stepConfigJson).summarisationTemplateId ?? '')}
                    onChange={(e) =>
                      handleUpdateWorkflowStepConfigField(idx, 'summarisationTemplateId', Number(e.target.value || 0))
                    }
                    className="rounded border border-slate-300 px-2 py-1"
                  >
                    <option value="">Select summarisation template</option>
                    {summarisationTemplates.map((template) => (
                      <option
                        key={template.documentSummarisationTemplateId}
                        value={template.documentSummarisationTemplateId}
                      >
                        {template.summarisationName}
                      </option>
                    ))}
                  </select>
                  <input
                    value={String(parseStepConfig(step.stepConfigJson).prompt ?? '')}
                    onChange={(e) => handleUpdateWorkflowStepConfigField(idx, 'prompt', e.target.value)}
                    className="rounded border border-slate-300 px-2 py-1"
                    placeholder="Optional prompt override (uses template prompt when blank)"
                  />
                  <p className="text-[11px] text-slate-500">
                    Generated summary is stored for later steps as <span className="font-semibold">{`{summary}`}</span>
                    , <span className="font-semibold">{`{summarisation}`}</span>, or
                    <span className="font-semibold"> {`{field:summary}`}</span>.
                  </p>
                </div>
              )}
            </div>
          ))}
          <button type="button" onClick={handleAddWorkflowStep} className="rounded border border-slate-300 bg-white px-2 py-1">Add Step</button>
        </div>

        <div className="mt-3">
          <button type="button" onClick={handleSaveWorkflowRule} disabled={loading} className="rounded-lg bg-slate-900 px-3 py-1.5 text-white">{editingWorkflowRuleId ? 'Update Workflow Rule' : 'Save Workflow Rule'}</button>
          {editingWorkflowRuleId && <button type="button" onClick={resetWorkflowForm} className="ml-2 rounded-lg border border-slate-300 px-3 py-1.5">Cancel Edit</button>}
        </div>

        <div className="mt-3 rounded border border-slate-200 bg-white p-2">
          <p className="font-semibold">Saved Workflow Rules</p>
          {workflowRules.length === 0 ? <p className="mt-1 text-slate-500">No workflow rules configured yet.</p> : (
            <ul className="mt-2 space-y-2">
              {workflowRules.map((rule) => (
                <li key={rule.documentWorkflowRuleId} className="rounded border border-slate-200 bg-slate-50 p-2">
                  <div className="flex items-center justify-between">
                    <div>
                      <p className="font-medium">{rule.workflowName}</p>
                      <p className="text-[11px] text-slate-600">Label: {rule.classificationLabel} | Min: {rule.minimumScore} | Priority: {rule.priority}</p>
                    </div>
                    <div className="flex gap-1">
                      <button type="button" onClick={() => handleEditWorkflowRule(rule)} className="rounded border border-slate-300 px-2 py-0.5">Edit</button>
                      <button type="button" onClick={() => handleDeleteWorkflowRule(rule.documentWorkflowRuleId, rule.workflowName)} className="rounded border border-rose-200 bg-rose-50 px-2 py-0.5 text-rose-700">Delete</button>
                    </div>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="mt-3 rounded border border-slate-200 bg-white p-2">
          <p className="font-semibold">Test Workflow Rule</p>
          <div className="mt-2 grid gap-2">
            <select value={workflowTestRuleId ?? ''} onChange={(e) => setWorkflowTestRuleId(e.target.value ? Number(e.target.value) : null)} className="rounded border border-slate-300 px-2 py-1">
              <option value="">Select workflow rule</option>
              {workflowRules.map((rule) => <option key={rule.documentWorkflowRuleId} value={rule.documentWorkflowRuleId}>{rule.workflowName}</option>)}
            </select>
            <input type="file" accept=".pdf,.txt,.csv,.json,.xml,.log,.md" onChange={(e) => setWorkflowTestFile(e.target.files?.[0] ?? null)} className="rounded border border-slate-300 px-2 py-1" />
            <button type="button" onClick={handleRunWorkflowTest} disabled={loading} className="rounded border border-slate-300 bg-white px-3 py-1.5">Run Workflow Test</button>
          </div>
          {workflowTestFeedback && <p className="mt-2 text-[11px]">{workflowTestFeedback}</p>}
          {workflowTestResult && (
            <div className="mt-2 rounded border border-indigo-100 bg-indigo-50 p-2 text-[11px]">
              <p><span className="font-semibold">Eligibility:</span> {workflowTestResult.ruleEligible ? 'Eligible' : 'Not eligible'}</p>
              <p><span className="font-semibold">Classification:</span> {workflowTestResult.classificationLabel} ({workflowTestResult.classificationScore})</p>
              <p className="whitespace-pre-wrap"><span className="font-semibold">Reason:</span> {workflowTestResult.eligibilityReason}</p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default DocumentFlows;
