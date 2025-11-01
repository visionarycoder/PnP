# Solution Architect Radar (2025 Q4)

This radar provides a maturity view of industry best practices across specialties.  
Use it to guide adoption, trials, and assessments, while avoiding outdated practices.

---

## Quadrant View

### ADOPT

- **Software Architecture**: VBD, DDD, Clean/Hexagonal Architecture, ADRs
- **Security**: Zero Trust, OWASP Top 10, centralized secrets management
- **Cloud**: Managed services, IaC (Terraform/Bicep)
- **DevOps**: GitOps, CI/CD pipelines, immutable infrastructure
- **Data**: Lakehouse, ELT with dbt, event streaming
- **Integration**: API-first, OpenAPI/AsyncAPI, backward-compatible versioning
- **Observability**: OpenTelemetry, SRE golden signals

### TRIAL

- **Software Architecture**: Event Sourcing, CQRS
- **Security**: Confidential computing, automated threat modeling
- **Cloud**: Serverless-first, multi-cloud portability frameworks
- **DevOps**: Internal Developer Platforms (IDPs), policy-as-code
- **Data**: Data mesh, real-time analytics
- **Integration**: GraphQL, gRPC
- **Observability**: Observability-as-code, continuous profiling

### ASSESS

- **Software Architecture**: AI-assisted validation, WASM backends
- **Security**: Post-quantum cryptography, AI-driven anomaly detection
- **Cloud**: Sustainability-aware workload placement
- **DevOps**: AI-driven pipeline optimization
- **Data**: Data contracts, AI-native governance
- **Integration**: Event mesh, API monetization
- **Observability**: AIOps-driven remediation, business KPI observability

### HOLD

- **Software Architecture**: Big Ball of Mud, God classes
- **Security**: Hardcoded secrets, perimeter-only defenses
- **Cloud**: Lift-and-shift without modernization
- **DevOps**: Manual deployments, snowflake servers
- **Data**: ETL sprawl, unmanaged silos
- **Integration**: Point-to-point spaghetti integrations
- **Observability**: Infra-only monitoring, unstructured logs

---

## Visual Radar (Mermaid)

```mermaid
flowchart LR
    subgraph Q1 [ADOPT]
      QA1[Software Architecture: DDD, Clean/Hexagonal, ADRs]
      QA2[Security: Zero Trust, OWASP Top 10, Secrets mgmt]
      QA3[Cloud: Managed services, IaC]
      QA4[DevOps: GitOps, CI/CD, Immutable infra]
      QA5[Data: Lakehouse, ELT with dbt, Event streaming]
      QA6[Integration: API-first, OpenAPI/AsyncAPI, Versioning]
      QA7[Observability: OpenTelemetry, SRE golden signals]
    end

    subgraph Q2 [TRIAL]
      QT1[Software Architecture: Event Sourcing, CQRS]
      QT2[Security: Confidential computing, Threat modeling automation]
      QT3[Cloud: Serverless-first, Multi-cloud portability]
      QT4[DevOps: IDPs, Policy-as-code]
      QT5[Data: Data mesh, Real-time analytics]
      QT6[Integration: GraphQL, gRPC]
      QT7[Observability: Observability-as-code, Continuous profiling]
    end

    subgraph Q3 [ASSESS]
      QS1[Software Architecture: AI-assisted validation, WASM backends]
      QS2[Security: Post-quantum crypto, AI anomaly detection]
      QS3[Cloud: Sustainability-aware placement]
      QS4[DevOps: AI-driven pipeline optimization]
      QS5[Data: Data contracts, AI-native governance]
      QS6[Integration: Event mesh, API monetization]
      QS7[Observability: AIOps remediation, Business KPI obs]
    end

    subgraph Q4 [HOLD]
      QH1[Software Architecture: Big Ball of Mud, God classes]
      QH2[Security: Hardcoded secrets, Perimeter-only]
      QH3[Cloud: Lift-and-shift w/o modernization]
      QH4[DevOps: Manual deployments, Snowflake servers]
      QH5[Data: ETL sprawl, Unmanaged silos]
      QH6[Integration: Point-to-point spaghetti, Breaking changes]
      QH7[Observability: Infra-only metrics, Unstructured logs]
    end

    classDef adopt fill=#b7f5c7,stroke=#2f7,stroke-width=1px,color=#000;
    classDef trial fill=#cbe8ff,stroke=#39f,stroke-width=1px,color=#000;
    classDef assess fill=#fff1a8,stroke=#fc3,stroke-width=1px,color=#000;
    classDef hold fill=#ffc2c2,stroke=#f55,stroke-width=1px,color=#000;

    class Q1 adopt;
    class Q2 trial;
    class Q3 assess;
    class Q4 hold;
```

---

## Related Governance Docs

- [Branching Strategy Playbook](branching-strategy.md)
- [Quarterly Radar Review Checklist](quarterly-radar-review.md)
- [ADR Index](../architecture-decision-records/index.md)

## Capsules

Each specialty has a dedicated capsule with detailed best practices:

- [Software Architecture](./software-architecture/README.md)  
- [Security](./security/README.md)  
- [Cloud Architecture](./cloud-architecture/README.md)  
- [DevOps & Platform Engineering](./devops/README.md)  
- [Data & Analytics](./data-analytics/README.md)  
- [Integration & APIs](./integration/README.md)  
- [Observability](./observability/README.md)  
