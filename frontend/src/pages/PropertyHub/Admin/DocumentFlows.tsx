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
  type TagTypeResponseDto,
  type TenancyResponseDto,
  type TenantResponseDto,
} from '../../../services/propertyAdminService';

type EditableWorkflowStep = UpsertDocumentWorkflowStepRequest;
const OUTLOOK_CATEGORY_COLORS: Array<{ value: string; label: string; hex: string }> = [
  { value: 'Preset0', label: 'Red', hex: '#e74c3c' },
  { value: 'Preset1', label: 'Orange', hex: '#e67e22' },
  { value: 'Preset2', label: 'Brown', hex: '#a0522d' },
  { value: 'Preset3', label: 'Yellow', hex: '#f1c40f' },
  { value: 'Preset4', label: 'Green', hex: '#2ecc71' },
  { value: 'Preset5', label: 'Teal', hex: '#1abc9c' },
  { value: 'Preset6', label: 'Olive', hex: '#7d8c2f' },
  { value: 'Preset7', label: 'Blue', hex: '#3498db' },
  { value: 'Preset8', label: 'Purple', hex: '#9b59b6' },
  { value: 'Preset9', label: 'Cranberry', hex: '#c0392b' },
  { value: 'Preset10', label: 'Steel', hex: '#5d6d7e' },
  { value: 'Preset11', label: 'Dark Gray', hex: '#4d4d4d' },
  { value: 'Preset12', label: 'Dark Red', hex: '#8e2a20' },
  { value: 'Preset13', label: 'Dark Orange', hex: '#a04a00' },
  { value: 'Preset14', label: 'Dark Brown', hex: '#5c4033' },
  { value: 'Preset15', label: 'Dark Yellow', hex: '#9a7d0a' },
  { value: 'Preset16', label: 'Dark Green', hex: '#1e8449' },
  { value: 'Preset17', label: 'Dark Teal', hex: '#117a65' },
  { value: 'Preset18', label: 'Dark Olive', hex: '#556b2f' },
  { value: 'Preset19', label: 'Dark Blue', hex: '#1f4e79' },
  { value: 'Preset20', label: 'Dark Purple', hex: '#6c3483' },
  { value: 'Preset21', label: 'Black', hex: '#2c3e50' },
  { value: 'Preset22', label: 'Gray', hex: '#95a5a6' },
  { value: 'Preset23', label: 'Neutral', hex: '#bdc3c7' },
  { value: 'Preset24', label: 'Light Gray', hex: '#dfe6e9' },
];

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
  const [tenancies, setTenancies] = useState<TenancyResponseDto[]>([]);
  const [journalTypes, setJournalTypes] = useState<JournalTypeDto[]>([]);
  const [contactLogTypes, setContactLogTypes] = useState<ContactLogTypeDto[]>([]);
  const [tagTypes, setTagTypes] = useState<TagTypeResponseDto[]>([]);

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
    if (raw.toLowerCase().includes('404')) {
      return 'Workflow API is unavailable on the backend deployment.';
    }
    if (raw.toLowerCase().includes('status: 400')) {
      return 'Save failed (400). Check Min score is 0–1 and Priority is a whole number (e.g. 100), not 0.28.';
    }
    return raw;
  }, []);

  const availableClassificationLabels = useMemo(() => {
    const labels = labelSets
      .flatMap((set) => set.labels ?? [])
      .map((label) => (label.classificationLabel ?? '').trim())
      .filter((label) => label.length > 0);
    return Array.from(new Set(labels)).sort((a, b) => a.localeCompare(b));
  }, [labelSets]);

  const refreshExtractionTemplates = useCallback(async () => {
    const loadedExtractionTemplates = await documentHubService.getExtractionTemplates();
    setExtractionTemplates(loadedExtractionTemplates);
  }, []);

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
        loadedTenancies,
        loadedJournalTypes,
        loadedContactLogTypes,
        loadedTagTypes,
      ] = await Promise.all([
        documentHubService.getWorkflowRules(),
        documentHubService.getLabelSets(),
        documentHubService.getExtractionTemplates(),
        documentHubService.getSummarisationTemplates(),
        propertyAdminService.getPropertyGroups(),
        propertyAdminService.getProperties(),
        propertyAdminService.getTenants(),
        propertyAdminService.getTenancies(),
        propertyAdminService.getJournalTypes(),
        propertyAdminService.getContactLogTypes(),
        propertyAdminService.getTagTypes(),
      ]);
      setWorkflowRules(loadedRules);
      setLabelSets(loadedLabelSets);
      setExtractionTemplates(loadedExtractionTemplates);
      setSummarisationTemplates(loadedSummarisationTemplates);
      setPropertyGroups(loadedPropertyGroups);
      setProperties(loadedProperties);
      setTenants(loadedTenants);
      setTenancies(loadedTenancies);
      setJournalTypes(loadedJournalTypes);
      setContactLogTypes(loadedContactLogTypes);
      setTagTypes(loadedTagTypes);
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

  const handleEditWorkflowRule = async (rule: DocumentWorkflowRuleDto) => {
    try {
      await refreshExtractionTemplates();
    } catch {
      // Keep editing flow resilient even if refresh fails.
    }
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
    if (loading) {
      return;
    }
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
    if (!Number.isFinite(workflowMinimumScore) || workflowMinimumScore < 0 || workflowMinimumScore > 1) {
      setFeedback('Min score must be between 0 and 1. Use 0 for Unclassified workflows.');
      return;
    }
    if (!Number.isInteger(workflowPriority) || workflowPriority < 1) {
      setFeedback('Priority must be a whole number of 1 or higher (e.g. 100). It is not the confidence score.');
      return;
    }

    setLoading(true);
    setFeedback('Saving workflow rule...');
    try {
      const expectedStepCount = steps.length;
      let savedRule: DocumentWorkflowRuleDto;
      if (editingWorkflowRuleId) {
        savedRule = await documentHubService.updateWorkflowRule(editingWorkflowRuleId, {
          workflowName: workflowName.trim(),
          classificationLabel: workflowClassificationLabel.trim(),
          minimumScore: workflowMinimumScore,
          priority: workflowPriority,
          stopOnFailure: workflowStopOnFailure,
          steps,
        });
      } else {
        savedRule = await documentHubService.createWorkflowRule({
          workflowName: workflowName.trim(),
          classificationLabel: workflowClassificationLabel.trim(),
          minimumScore: workflowMinimumScore,
          priority: workflowPriority,
          stopOnFailure: workflowStopOnFailure,
          steps,
        });
      }

      const persistedSteps = savedRule.steps?.length ?? 0;
      if (persistedSteps < expectedStepCount) {
        setFeedback(
          `Workflow saved but only ${persistedSteps}/${expectedStepCount} steps were persisted. Your current form was kept so you can retry.`
        );
        await loadData();
        return;
      }

      try {
        await loadData();
        resetWorkflowForm();
        setFeedback('Workflow rule saved.');
      } catch {
        setFeedback('Workflow rule saved, but the page failed to refresh data. Your form was kept so you can retry safely.');
      }
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

  const parseTagIdListFromCsv = (csvRaw: string): number[] => {
    return csvRaw
      .split(',')
      .map((part) => Number(part.trim()))
      .filter((id) => Number.isInteger(id) && id > 0);
  };

  const toTagCsv = (ids: number[]): string => Array.from(new Set(ids)).sort((a, b) => a - b).join(',');

  const toggleWorkflowStepTagType = (idx: number, tagTypeId: number, checked: boolean) => {
    setWorkflowSteps((prev) =>
      prev.map((step, index) => {
        if (index !== idx) return step;
        const config = parseStepConfig(step.stepConfigJson);
        const current = parseTagIdListFromCsv(String(config.tagTypeIdsCsv ?? ''));
        const next = checked ? [...current, tagTypeId] : current.filter((id) => id !== tagTypeId);
        config.tagTypeIdsCsv = toTagCsv(next);
        return { ...step, stepOrder: index + 1, stepConfigJson: stringifyStepConfig(config) };
      })
    );
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
          <label className="block">
            <span className="mb-1 block text-[11px] font-medium text-slate-600">Min score (0–1)</span>
            <input
              type="number"
              min={0}
              max={1}
              step={0.01}
              value={workflowMinimumScore}
              onChange={(e) => {
                const raw = e.target.value;
                setWorkflowMinimumScore(raw === '' ? 0 : Number(raw));
              }}
              className="w-full rounded border border-slate-300 px-2 py-1"
            />
          </label>
          <label className="block">
            <span className="mb-1 block text-[11px] font-medium text-slate-600">Priority (whole number)</span>
            <input
              type="number"
              min={1}
              step={1}
              value={workflowPriority}
              onChange={(e) => {
                const parsed = Number(e.target.value);
                setWorkflowPriority(Number.isFinite(parsed) ? Math.max(1, Math.round(parsed)) : 100);
              }}
              className="w-full rounded border border-slate-300 px-2 py-1"
            />
          </label>
        </div>
        {workflowClassificationLabel.trim().toLowerCase() === 'unclassified' && workflowMinimumScore > 0 && (
          <p className="mt-2 text-[11px] text-amber-700">
            Unclassified emails usually score below 0.28. Set Min score to <span className="font-semibold">0</span> so this workflow can run.
          </p>
        )}
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
              {step.stepType === 'MarkCompleted' && (
                <p className="mt-1 text-[11px] text-slate-500">
                  Applies Outlook &quot;Mark Complete&quot; on the follow-up flag (not an Outlook Category).
                </p>
              )}
              {step.stepType === 'SetCategory' && (() => {
                const setCategoryConfig = parseStepConfig(step.stepConfigJson);
                const selectedCategoryColor = String(setCategoryConfig.categoryColor ?? '');
                const selectedColorMeta = OUTLOOK_CATEGORY_COLORS.find((c) => c.value === selectedCategoryColor);

                return (
                  <div className="mt-2 grid gap-1 md:grid-cols-2">
                    <input
                      value={String(setCategoryConfig.category ?? '')}
                      onChange={(e) => handleUpdateWorkflowStepConfigField(idx, 'category', e.target.value)}
                      className="w-full rounded border border-slate-300 px-2 py-1"
                      placeholder='Optional override category (blank = classification label)'
                    />
                    <select
                      value={selectedCategoryColor}
                      onChange={(e) => handleUpdateWorkflowStepConfigField(idx, 'categoryColor', e.target.value)}
                      className="w-full rounded border border-slate-300 px-2 py-1"
                    >
                      <option value="">Default color / keep existing</option>
                      {OUTLOOK_CATEGORY_COLORS.map((color) => (
                        <option key={color.value} value={color.value}>
                          {color.value} - {color.label}
                        </option>
                      ))}
                    </select>
                    <div className="md:col-span-2 flex items-center gap-2 text-[11px] text-slate-600">
                      <span
                        className="inline-block h-3.5 w-3.5 rounded border border-slate-300"
                        style={{ backgroundColor: selectedColorMeta?.hex ?? 'transparent' }}
                      />
                      <span>
                        {selectedColorMeta
                          ? `Selected color preview: ${selectedColorMeta.value} (${selectedColorMeta.label})`
                          : 'No category color selected (existing/default Outlook color will be used).'}
                      </span>
                    </div>
                    <p className="md:col-span-2 text-[10px] text-slate-500">
                      SetCategory now ensures the category exists in Outlook Master Categories. Choosing a color applies it to the master category.
                    </p>
                  </div>
                );
              })()}
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
                    const filteredTenancies = selectedPropertyGroupId
                      ? tenancies.filter((t) => t.propertyId === Number(config.propertyId ?? 0))
                      : tenancies;

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
                          <label className="block text-[11px] font-medium text-slate-600">Tenancy</label>
                          <select
                            value={normalizeIdForSelect(config.tenancyId)}
                            onChange={(e) =>
                              handleUpdateWorkflowStepConfigField(idx, 'tenancyId', e.target.value === '' ? '' : Number(e.target.value))
                            }
                            className="mt-1 w-full rounded border border-slate-300 px-2 py-1"
                          >
                            <option value="">Select tenancy</option>
                            {filteredTenancies.map((t) => (
                              <option key={t.tenancyId} value={t.tenancyId}>
                                {t.propertyName} ({new Date(t.startDate).toLocaleDateString()})
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
                            These selections are written as integer IDs for `tblJournalLog` (journalTypeId, journalSubTypeId, propertyId, tenancyId, tenantId).
                          </p>
                        </div>

                        <input
                          value={String(config.journalReferenceTemplate ?? '')}
                          onChange={(e) => handleUpdateWorkflowStepConfigField(idx, 'journalReferenceTemplate', e.target.value)}
                          className="rounded border border-slate-300 px-2 py-1"
                          placeholder="journalReferenceTemplate (supports tokens)"
                        />
                        <input
                          value={String(config.transactionDateOffsetDays ?? '')}
                          onChange={(e) => handleUpdateWorkflowStepConfigField(idx, 'transactionDateOffsetDays', e.target.value === '' ? '' : Number(e.target.value))}
                          className="rounded border border-slate-300 px-2 py-1"
                          placeholder="transactionDateOffsetDays (e.g. 0, 1, -1)"
                        />
                        <input
                          value={String(config.journalAmountRandTemplate ?? '')}
                          onChange={(e) => handleUpdateWorkflowStepConfigField(idx, 'journalAmountRandTemplate', e.target.value)}
                          className="rounded border border-slate-300 px-2 py-1"
                          placeholder="journalAmountRandTemplate (e.g. {field:total_incl_vat} or {field:total_incl_vat}*3.8%)"
                        />
                        <div>
                          <label className="block text-[11px] font-medium text-slate-600">ZAR-&gt;GBP rate source</label>
                          <select
                            value={String(config.zarGbpRateSource ?? 'live')}
                            onChange={(e) => handleUpdateWorkflowStepConfigField(idx, 'zarGbpRateSource', e.target.value)}
                            className="mt-1 w-full rounded border border-slate-300 px-2 py-1"
                          >
                            <option value="live">Live (fetch latest ZAR-&gt;GBP during flow run)</option>
                            <option value="template">Template/manual value</option>
                          </select>
                        </div>
                        <input
                          value={String(config.journalAmountGbpTemplate ?? '')}
                          onChange={(e) => handleUpdateWorkflowStepConfigField(idx, 'journalAmountGbpTemplate', e.target.value)}
                          className="rounded border border-slate-300 px-2 py-1"
                          placeholder="journalAmountGbpTemplate (optional override, usually auto-calculated)"
                        />
                        <input
                          value={String(config.zarGbpCurrencyExchangeRateTemplate ?? '')}
                          onChange={(e) => handleUpdateWorkflowStepConfigField(idx, 'zarGbpCurrencyExchangeRateTemplate', e.target.value)}
                          className="md:col-span-2 rounded border border-slate-300 px-2 py-1"
                          placeholder="zarGbpCurrencyExchangeRateTemplate (optional override; blank = fetch live ZAR->GBP)"
                        />

                        <input
                          value={String(config.descriptionTemplate ?? '')}
                          onChange={(e) =>
                            handleUpdateWorkflowStepConfigFields(idx, {
                              descriptionTemplate: e.target.value,
                              journalDescriptionTemplate: e.target.value,
                            })
                          }
                          className="md:col-span-2 rounded border border-slate-300 px-2 py-1"
                          placeholder="descriptionTemplate (supports {field:<name>}, {classificationLabel}, {classificationScore}, {summary})"
                        />
                        <label className="md:col-span-2 inline-flex items-center gap-2 text-[11px] text-slate-700">
                          <input
                            type="checkbox"
                            checked={Boolean(config.attachEmailAttachments)}
                            onChange={(e) => handleUpdateWorkflowStepConfigField(idx, 'attachEmailAttachments', e.target.checked)}
                          />
                          Add rows to `tblJournalLogAttachment` for each email attachment.
                        </label>
                        <label className="md:col-span-2 inline-flex items-center gap-2 text-[11px] text-slate-700">
                          <input
                            type="checkbox"
                            checked={Boolean(config.addToCalendar)}
                            onChange={(e) => handleUpdateWorkflowStepConfigField(idx, 'addToCalendar', e.target.checked)}
                          />
                          Add this journal to calendar appointments.
                        </label>
                        <input
                          value={String(config.calendarDateOffsetDays ?? '')}
                          onChange={(e) => handleUpdateWorkflowStepConfigField(idx, 'calendarDateOffsetDays', e.target.value === '' ? '' : Number(e.target.value))}
                          className="rounded border border-slate-300 px-2 py-1"
                          placeholder="calendarDateOffsetDays (e.g. 0, 1, -1)"
                        />
                        <input
                          value={String(config.calendarTitleTemplate ?? '')}
                          onChange={(e) => handleUpdateWorkflowStepConfigField(idx, 'calendarTitleTemplate', e.target.value)}
                          className="rounded border border-slate-300 px-2 py-1"
                          placeholder="calendarTitleTemplate (optional)"
                        />
                        <input
                          value={String(config.calendarNotesTemplate ?? '')}
                          onChange={(e) => handleUpdateWorkflowStepConfigField(idx, 'calendarNotesTemplate', e.target.value)}
                          className="md:col-span-2 rounded border border-slate-300 px-2 py-1"
                          placeholder="calendarNotesTemplate (optional)"
                        />
                        <div className="md:col-span-2 rounded border border-slate-200 bg-slate-50 p-2">
                          <label className="block text-[11px] font-medium text-slate-600">Tag types (fixed IDs)</label>
                          {tagTypes.length === 0 ? (
                            <p className="mt-1 text-[10px] text-slate-500">No tag types available yet.</p>
                          ) : (
                            <div className="mt-1 grid gap-1 md:grid-cols-2">
                              {tagTypes.map((tagType) => {
                                const selected = parseTagIdListFromCsv(String(config.tagTypeIdsCsv ?? '')).includes(tagType.tagTypeId);
                                return (
                                  <label key={tagType.tagTypeId} className="inline-flex items-center gap-2 rounded border border-slate-200 bg-white px-2 py-1">
                                    <input
                                      type="checkbox"
                                      checked={selected}
                                      onChange={(e) => toggleWorkflowStepTagType(idx, tagType.tagTypeId, e.target.checked)}
                                    />
                                    <span
                                      className="inline-block h-2.5 w-2.5 rounded border border-slate-300"
                                      style={{ backgroundColor: tagType.color || 'transparent' }}
                                    />
                                    <span className="text-[11px] text-slate-700">
                                      {tagType.tagTypeName} (ID: {tagType.tagTypeId})
                                    </span>
                                  </label>
                                );
                              })}
                            </div>
                          )}
                          <p className="mt-1 text-[10px] text-slate-500">
                            Uses <span className="font-semibold">tagTypeIdsCsv</span>. If <span className="font-semibold">tagTypeIdsCsvTemplate</span> is provided, template output takes precedence.
                          </p>
                        </div>
                        <details className="md:col-span-2 rounded border border-slate-200 bg-slate-50 p-2">
                          <summary className="cursor-pointer text-[11px] font-medium text-slate-700">
                            Advanced tag options (template override)
                          </summary>
                          <input
                            value={String(config.tagTypeIdsCsvTemplate ?? '')}
                            onChange={(e) => handleUpdateWorkflowStepConfigField(idx, 'tagTypeIdsCsvTemplate', e.target.value)}
                            className="mt-2 w-full rounded border border-slate-300 px-2 py-1"
                            placeholder="tagTypeIdsCsvTemplate (e.g. 3,5 or {field:journal_tag_ids})"
                          />
                          <p className="mt-1 text-[10px] text-slate-500">
                            Use this when tags should come dynamically from extracted fields. When this resolves to a value, it overrides fixed tag selections.
                          </p>
                        </details>
                        <input
                          value={String(config.attachmentAddedByTemplate ?? '')}
                          onChange={(e) => handleUpdateWorkflowStepConfigField(idx, 'attachmentAddedByTemplate', e.target.value)}
                          className="md:col-span-2 rounded border border-slate-300 px-2 py-1"
                          placeholder="attachmentAddedByTemplate (optional)"
                        />
                        <p className="md:col-span-2 text-[10px] text-slate-500">
                          Token helpers: {'{field:meter_number}'}, {'{field:account_number}'}, {'{field:invoice_total}'},{' '}
                          {'{extractionJson}'}, {'{classificationLabel}'}, {'{classificationScore}'}, {'{summary}'}.
                          If using {'{field:...}'} for amounts, ensure <span className="font-semibold">RunExtraction</span> is before <span className="font-semibold">CreateJournalLog</span> in step order.
                          Numeric templates support math: +, -, *, /, brackets, and percentages (e.g. {'{field:total_incl_vat}*3.8%'}). To trigger live FX fetch, set rate source to <span className="font-semibold">Live</span> and provide a ZAR amount template. The flow then fetches current ZAR-&gt;GBP and calculates GBP as ZAR x rate.
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
                          value={String(config.contactDateOffsetDays ?? '')}
                          onChange={(e) => handleUpdateWorkflowStepConfigField(idx, 'contactDateOffsetDays', e.target.value === '' ? '' : Number(e.target.value))}
                          className="rounded border border-slate-300 px-2 py-1"
                          placeholder="contactDateOffsetDays (e.g. 0, 1, -1)"
                        />
                        <input
                          value={String(config.contactIdTemplate ?? '')}
                          onChange={(e) => handleUpdateWorkflowStepConfigField(idx, 'contactIdTemplate', e.target.value)}
                          className="rounded border border-slate-300 px-2 py-1"
                          placeholder="contactIdTemplate (optional)"
                        />
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
                        <label className="md:col-span-2 inline-flex items-center gap-2 text-[11px] text-slate-700">
                          <input
                            type="checkbox"
                            checked={Boolean(config.attachEmailAttachments)}
                            onChange={(e) => handleUpdateWorkflowStepConfigField(idx, 'attachEmailAttachments', e.target.checked)}
                          />
                          Add rows to `tblContactLogAttachment` for each email attachment.
                        </label>
                        <label className="md:col-span-2 inline-flex items-center gap-2 text-[11px] text-slate-700">
                          <input
                            type="checkbox"
                            checked={Boolean(config.addToCalendar)}
                            onChange={(e) => handleUpdateWorkflowStepConfigField(idx, 'addToCalendar', e.target.checked)}
                          />
                          Add this contact log to calendar appointments.
                        </label>
                        <input
                          value={String(config.calendarDateOffsetDays ?? '')}
                          onChange={(e) => handleUpdateWorkflowStepConfigField(idx, 'calendarDateOffsetDays', e.target.value === '' ? '' : Number(e.target.value))}
                          className="rounded border border-slate-300 px-2 py-1"
                          placeholder="calendarDateOffsetDays (e.g. 0, 1, -1)"
                        />
                        <input
                          value={String(config.calendarTitleTemplate ?? '')}
                          onChange={(e) => handleUpdateWorkflowStepConfigField(idx, 'calendarTitleTemplate', e.target.value)}
                          className="rounded border border-slate-300 px-2 py-1"
                          placeholder="calendarTitleTemplate (optional)"
                        />
                        <input
                          value={String(config.calendarNotesTemplate ?? '')}
                          onChange={(e) => handleUpdateWorkflowStepConfigField(idx, 'calendarNotesTemplate', e.target.value)}
                          className="md:col-span-2 rounded border border-slate-300 px-2 py-1"
                          placeholder="calendarNotesTemplate (optional)"
                        />
                        <div className="md:col-span-2 rounded border border-slate-200 bg-slate-50 p-2">
                          <label className="block text-[11px] font-medium text-slate-600">Tag types (fixed IDs)</label>
                          {tagTypes.length === 0 ? (
                            <p className="mt-1 text-[10px] text-slate-500">No tag types available yet.</p>
                          ) : (
                            <div className="mt-1 grid gap-1 md:grid-cols-2">
                              {tagTypes.map((tagType) => {
                                const selected = parseTagIdListFromCsv(String(config.tagTypeIdsCsv ?? '')).includes(tagType.tagTypeId);
                                return (
                                  <label key={tagType.tagTypeId} className="inline-flex items-center gap-2 rounded border border-slate-200 bg-white px-2 py-1">
                                    <input
                                      type="checkbox"
                                      checked={selected}
                                      onChange={(e) => toggleWorkflowStepTagType(idx, tagType.tagTypeId, e.target.checked)}
                                    />
                                    <span
                                      className="inline-block h-2.5 w-2.5 rounded border border-slate-300"
                                      style={{ backgroundColor: tagType.color || 'transparent' }}
                                    />
                                    <span className="text-[11px] text-slate-700">
                                      {tagType.tagTypeName} (ID: {tagType.tagTypeId})
                                    </span>
                                  </label>
                                );
                              })}
                            </div>
                          )}
                          <p className="mt-1 text-[10px] text-slate-500">
                            Uses <span className="font-semibold">tagTypeIdsCsv</span>. If <span className="font-semibold">tagTypeIdsCsvTemplate</span> is provided, template output takes precedence.
                          </p>
                        </div>
                        <details className="md:col-span-2 rounded border border-slate-200 bg-slate-50 p-2">
                          <summary className="cursor-pointer text-[11px] font-medium text-slate-700">
                            Advanced tag options (template override)
                          </summary>
                          <input
                            value={String(config.tagTypeIdsCsvTemplate ?? '')}
                            onChange={(e) => handleUpdateWorkflowStepConfigField(idx, 'tagTypeIdsCsvTemplate', e.target.value)}
                            className="mt-2 w-full rounded border border-slate-300 px-2 py-1"
                            placeholder="tagTypeIdsCsvTemplate (e.g. 3,5 or {field:contact_tag_ids})"
                          />
                          <p className="mt-1 text-[10px] text-slate-500">
                            Use this when tags should come dynamically from extracted fields. When this resolves to a value, it overrides fixed tag selections.
                          </p>
                        </details>
                        <input
                          value={String(config.attachmentDescriptionTemplate ?? '')}
                          onChange={(e) => handleUpdateWorkflowStepConfigField(idx, 'attachmentDescriptionTemplate', e.target.value)}
                          className="md:col-span-2 rounded border border-slate-300 px-2 py-1"
                          placeholder="attachmentDescriptionTemplate (optional, defaults to attachment filename)"
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
                  {(() => {
                    const config = parseStepConfig(step.stepConfigJson);
                    const selectedTemplateId = Number(config.extractionTemplateId ?? 0);
                    const selectedTemplate = extractionTemplates.find(
                      (template) => template.documentExtractionTemplateId === selectedTemplateId
                    );
                    const templateFieldTokens = (selectedTemplate?.fields ?? [])
                      .map((field) => field.fieldName?.trim() ?? '')
                      .filter((name) => name.length > 0)
                      .map((name) => `{field:${name.toLowerCase().replace(/[^a-z0-9]+/g, '_').replace(/^_+|_+$/g, '')}}`)
                      .filter((token) => token !== '{field:}');

                    return (
                      <>
                  <div className="flex items-center gap-2">
                    <select
                      value={String(config.extractionTemplateId ?? '')}
                      onChange={(e) => handleUpdateWorkflowStepConfigField(idx, 'extractionTemplateId', Number(e.target.value || 0))}
                      className="flex-1 rounded border border-slate-300 px-2 py-1"
                    >
                      <option value="">Select extraction template</option>
                      {extractionTemplates.map((template) => (
                        <option key={template.documentExtractionTemplateId} value={template.documentExtractionTemplateId}>
                          {template.extractionTemplateName}
                        </option>
                      ))}
                    </select>
                    <button
                      type="button"
                      onClick={async () => {
                        try {
                          await refreshExtractionTemplates();
                          setFeedback('Extraction templates refreshed.');
                        } catch (error) {
                          setFeedback(getFriendlyError(error));
                        }
                      }}
                      className="rounded border border-slate-300 bg-white px-2 py-1 text-[11px]"
                    >
                      Refresh fields
                    </button>
                  </div>
                  <p className="text-[11px] text-slate-500">
                    This runs entity extraction and makes fields available to later steps as {`{field:<field_name>}`}.
                  </p>
                        {selectedTemplate && (
                          <p className="text-[11px] text-indigo-700">
                            Template fields available in later steps: {templateFieldTokens.length > 0 ? templateFieldTokens.join(', ') : 'No fields configured yet.'}
                          </p>
                        )}
                      </>
                    );
                  })()}
                </div>
              )}
              {step.stepType === 'RunSummarisation' && (
                <div className="mt-2 grid gap-1">
                  {(() => {
                    const config = parseStepConfig(step.stepConfigJson);
                    const selectedTemplateId = Number(config.summarisationTemplateId ?? 0);
                    const selectedTemplate = summarisationTemplates.find(
                      (template) => template.documentSummarisationTemplateId === selectedTemplateId
                    );

                    return (
                      <>
                  <select
                    value={String(config.summarisationTemplateId ?? '')}
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
                        {selectedTemplate && (
                          <p className="text-[11px] text-indigo-700">
                            Selected template: {selectedTemplate.summarisationName}
                            {selectedTemplate.summarisationDescription ? ` - ${selectedTemplate.summarisationDescription}` : ''}.
                          </p>
                        )}
                      </>
                    );
                  })()}
                </div>
              )}
            </div>
          ))}
          <button type="button" onClick={handleAddWorkflowStep} className="rounded border border-slate-300 bg-white px-2 py-1">Add Step</button>
        </div>

        <div className="mt-3">
          <button type="button" onClick={handleSaveWorkflowRule} disabled={loading} className="rounded-lg bg-slate-900 px-3 py-1.5 text-white">
            {loading ? 'Saving...' : editingWorkflowRuleId ? 'Update Workflow Rule' : 'Save Workflow Rule'}
          </button>
          {editingWorkflowRuleId && <button type="button" onClick={resetWorkflowForm} className="ml-2 rounded-lg border border-slate-300 px-3 py-1.5">Cancel Edit</button>}
          {feedback && <p className="mt-2 text-[11px] text-slate-600">{feedback}</p>}
        </div>

        <div className="mt-4 rounded-lg border border-indigo-200 bg-indigo-50/60 p-3 shadow-sm">
          <p className="flex items-center gap-2 font-semibold text-indigo-900">
            Saved Workflow Rules
            <span className="inline-flex items-center rounded-full border border-indigo-200 bg-white px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-indigo-700">
              Saved
            </span>
          </p>
          {workflowRules.length === 0 ? <p className="mt-1 text-slate-500">No workflow rules configured yet.</p> : (
            <ul className="mt-2 space-y-2">
              {workflowRules.map((rule) => (
                <li key={rule.documentWorkflowRuleId} className="rounded-md border border-indigo-100 bg-white p-2.5 shadow-sm transition hover:border-indigo-200 hover:shadow">
                  <div className="flex items-center justify-between">
                    <div>
                      <p className="font-medium text-slate-900">{rule.workflowName}</p>
                      <p className="text-[11px] text-slate-600">Label: {rule.classificationLabel} | Min: {rule.minimumScore} | Priority: {rule.priority}</p>
                    </div>
                    <div className="flex gap-1">
                      <button type="button" onClick={() => { void handleEditWorkflowRule(rule); }} className="rounded border border-slate-300 bg-white px-2 py-0.5 text-slate-700 hover:border-slate-400">Edit</button>
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
