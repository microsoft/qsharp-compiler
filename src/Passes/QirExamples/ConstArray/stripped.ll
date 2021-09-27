; ModuleID = 'runtime.cpp'
source_filename = "runtime.cpp"
target datalayout = "e-m:o-p270:32:32-p271:32:32-p272:64:64-i64:64-f80:128-n8:16:32:64-S128"
target triple = "x86_64-apple-macosx11.0.0"

%"class.std::__1::unordered_map" = type { %"class.std::__1::__hash_table" }
%"class.std::__1::__hash_table" = type <{ %"class.std::__1::unique_ptr", %"class.std::__1::__compressed_pair.4", %"class.std::__1::__compressed_pair.9", %"class.std::__1::__compressed_pair.11", [4 x i8] }>
%"class.std::__1::unique_ptr" = type { %"class.std::__1::__compressed_pair" }
%"class.std::__1::__compressed_pair" = type { %"struct.std::__1::__compressed_pair_elem", %"struct.std::__1::__compressed_pair_elem.0" }
%"struct.std::__1::__compressed_pair_elem" = type { %"struct.std::__1::__hash_node_base"** }
%"struct.std::__1::__hash_node_base" = type { %"struct.std::__1::__hash_node_base"* }
%"struct.std::__1::__compressed_pair_elem.0" = type { %"class.std::__1::__bucket_list_deallocator" }
%"class.std::__1::__bucket_list_deallocator" = type { %"class.std::__1::__compressed_pair.1" }
%"class.std::__1::__compressed_pair.1" = type { %"struct.std::__1::__compressed_pair_elem.2" }
%"struct.std::__1::__compressed_pair_elem.2" = type { i64 }
%"class.std::__1::__compressed_pair.4" = type { %"struct.std::__1::__compressed_pair_elem.5" }
%"struct.std::__1::__compressed_pair_elem.5" = type { %"struct.std::__1::__hash_node_base" }
%"class.std::__1::__compressed_pair.9" = type { %"struct.std::__1::__compressed_pair_elem.2" }
%"class.std::__1::__compressed_pair.11" = type { %"struct.std::__1::__compressed_pair_elem.12" }
%"struct.std::__1::__compressed_pair_elem.12" = type { float }
%class.Array = type { i64, i64, i8* }
%"struct.std::__1::__default_init_tag" = type { i8 }
%"struct.std::__1::__compressed_pair_elem.3" = type { i8 }
%"class.std::__1::allocator" = type { i8 }
%"struct.std::__1::__value_init_tag" = type { i8 }
%"struct.std::__1::__compressed_pair_elem.6" = type { i8 }
%"class.std::__1::allocator.7" = type { i8 }
%"struct.std::__1::__compressed_pair_elem.10" = type { i8 }
%"class.std::__1::__unordered_map_hasher" = type { i8 }
%"struct.std::__1::hash" = type { i8 }
%"struct.std::__1::__compressed_pair_elem.13" = type { i8 }
%"class.std::__1::__unordered_map_equal" = type { i8 }
%"struct.std::__1::equal_to" = type { i8 }
%"struct.std::__1::__hash_node" = type { %"struct.std::__1::__hash_node_base", i64, %"struct.std::__1::__hash_value_type" }
%"struct.std::__1::__hash_value_type" = type { %"struct.std::__1::pair" }
%"struct.std::__1::pair" = type <{ %class.Array*, i32, [4 x i8] }>
%"struct.std::__1::integral_constant" = type { i8 }
%"struct.std::__1::__has_destroy" = type { i8 }

@alias_count = global %"class.std::__1::unordered_map" zeroinitializer, align 8
@__dso_handle = external hidden global i8
@ref_count = global %"class.std::__1::unordered_map" zeroinitializer, align 8
@llvm.global_ctors = appending global [1 x { i32, void ()*, i8* }] [{ i32, void ()*, i8* } { i32 65535, void ()* @_GLOBAL__sub_I_runtime.cpp, i8* null }]

; Function Attrs: noinline ssp uwtable
define internal void @__cxx_global_var_init()  {
  call void @_ZNSt3__113unordered_mapIP5ArrayiNS_4hashIS2_EENS_8equal_toIS2_EENS_9allocatorINS_4pairIKS2_iEEEEEC1Ev(%"class.std::__1::unordered_map"* @alias_count) #2
  %1 = call i32 @__cxa_atexit(void (i8*)* bitcast (void (%"class.std::__1::unordered_map"*)* @_ZNSt3__113unordered_mapIP5ArrayiNS_4hashIS2_EENS_8equal_toIS2_EENS_9allocatorINS_4pairIKS2_iEEEEED1Ev to void (i8*)*), i8* bitcast (%"class.std::__1::unordered_map"* @alias_count to i8*), i8* @__dso_handle) #2
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__113unordered_mapIP5ArrayiNS_4hashIS2_EENS_8equal_toIS2_EENS_9allocatorINS_4pairIKS2_iEEEEEC1Ev(%"class.std::__1::unordered_map"* %0) unnamed_addr  {
  %2 = alloca %"class.std::__1::unordered_map"*, align 8
  store %"class.std::__1::unordered_map"* %0, %"class.std::__1::unordered_map"** %2, align 8
  %3 = load %"class.std::__1::unordered_map"*, %"class.std::__1::unordered_map"** %2, align 8
  call void @_ZNSt3__113unordered_mapIP5ArrayiNS_4hashIS2_EENS_8equal_toIS2_EENS_9allocatorINS_4pairIKS2_iEEEEEC2Ev(%"class.std::__1::unordered_map"* %3) #2
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__113unordered_mapIP5ArrayiNS_4hashIS2_EENS_8equal_toIS2_EENS_9allocatorINS_4pairIKS2_iEEEEED1Ev(%"class.std::__1::unordered_map"* %0) unnamed_addr  {
  %2 = alloca %"class.std::__1::unordered_map"*, align 8
  store %"class.std::__1::unordered_map"* %0, %"class.std::__1::unordered_map"** %2, align 8
  %3 = load %"class.std::__1::unordered_map"*, %"class.std::__1::unordered_map"** %2, align 8
  call void @_ZNSt3__113unordered_mapIP5ArrayiNS_4hashIS2_EENS_8equal_toIS2_EENS_9allocatorINS_4pairIKS2_iEEEEED2Ev(%"class.std::__1::unordered_map"* %3) #2
  ret void
}

; Function Attrs: nounwind
declare i32 @__cxa_atexit(void (i8*)*, i8*, i8*) 

; Function Attrs: noinline ssp uwtable
define internal void @__cxx_global_var_init.1()  {
  call void @_ZNSt3__113unordered_mapIP5ArrayiNS_4hashIS2_EENS_8equal_toIS2_EENS_9allocatorINS_4pairIKS2_iEEEEEC1Ev(%"class.std::__1::unordered_map"* @ref_count) #2
  %1 = call i32 @__cxa_atexit(void (i8*)* bitcast (void (%"class.std::__1::unordered_map"*)* @_ZNSt3__113unordered_mapIP5ArrayiNS_4hashIS2_EENS_8equal_toIS2_EENS_9allocatorINS_4pairIKS2_iEEEEED1Ev to void (i8*)*), i8* bitcast (%"class.std::__1::unordered_map"* @ref_count to i8*), i8* @__dso_handle) #2
  ret void
}

; Function Attrs: noinline optnone ssp uwtable
define %class.Array* @__quantum__rt__array_create_1d(i32 %0, i64 %1)  personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*)  {
  %3 = alloca i32, align 4
  %4 = alloca i64, align 8
  %5 = alloca i8*, align 8
  %6 = alloca i32, align 4
  store i32 %0, i32* %3, align 4
  store i64 %1, i64* %4, align 8
  %7 = call noalias nonnull i8* @_Znwm(i64 24) #7
  %8 = bitcast i8* %7 to %class.Array*
  %9 = load i32, i32* %3, align 4
  %10 = sext i32 %9 to i64
  %11 = load i64, i64* %4, align 8
  invoke void @_ZN5ArrayC1Exx(%class.Array* %8, i64 %10, i64 %11)
          to label %12 unwind label %13

12:                                               ; preds = %2
  ret %class.Array* %8

13:                                               ; preds = %2
  %14 = landingpad { i8*, i32 }
          cleanup
  %15 = extractvalue { i8*, i32 } %14, 0
  store i8* %15, i8** %5, align 8
  %16 = extractvalue { i8*, i32 } %14, 1
  store i32 %16, i32* %6, align 4
  call void @_ZdlPv(i8* %7) #8
  br label %17

17:                                               ; preds = %13
  %18 = load i8*, i8** %5, align 8
  %19 = load i32, i32* %6, align 4
  %20 = insertvalue { i8*, i32 } undef, i8* %18, 0
  %21 = insertvalue { i8*, i32 } %20, i32 %19, 1
  resume { i8*, i32 } %21
}

; Function Attrs: nobuiltin allocsize(0)
declare nonnull i8* @_Znwm(i64) 

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZN5ArrayC1Exx(%class.Array* %0, i64 %1, i64 %2) unnamed_addr  {
  %4 = alloca %class.Array*, align 8
  %5 = alloca i64, align 8
  %6 = alloca i64, align 8
  store %class.Array* %0, %class.Array** %4, align 8
  store i64 %1, i64* %5, align 8
  store i64 %2, i64* %6, align 8
  %7 = load %class.Array*, %class.Array** %4, align 8
  %8 = load i64, i64* %5, align 8
  %9 = load i64, i64* %6, align 8
  call void @_ZN5ArrayC2Exx(%class.Array* %7, i64 %8, i64 %9)
  ret void
}

declare i32 @__gxx_personality_v0(...)

; Function Attrs: nobuiltin nounwind
declare void @_ZdlPv(i8*) 

; Function Attrs: noinline nounwind optnone ssp uwtable
define i8* @__quantum__rt__array_get_element_ptr_1d(%class.Array* %0, i64 %1)  {
  %3 = alloca %class.Array*, align 8
  %4 = alloca i64, align 8
  store %class.Array* %0, %class.Array** %3, align 8
  store i64 %1, i64* %4, align 8
  %5 = load %class.Array*, %class.Array** %3, align 8
  %6 = getelementptr inbounds %class.Array, %class.Array* %5, i32 0, i32 2
  %7 = load i8*, i8** %6, align 8
  %8 = load i64, i64* %4, align 8
  %9 = load %class.Array*, %class.Array** %3, align 8
  %10 = getelementptr inbounds %class.Array, %class.Array* %9, i32 0, i32 0
  %11 = load i64, i64* %10, align 8
  %12 = mul nsw i64 %8, %11
  %13 = getelementptr inbounds i8, i8* %7, i64 %12
  ret i8* %13
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define void @__quantum__rt__qubit_release_array(%class.Array* %0)  {
  %2 = alloca %class.Array*, align 8
  store %class.Array* %0, %class.Array** %2, align 8
  %3 = load %class.Array*, %class.Array** %2, align 8
  %4 = icmp eq %class.Array* %3, null
  br i1 %4, label %7, label %5

5:                                                ; preds = %1
  call void @_ZN5ArrayD1Ev(%class.Array* %3) #2
  %6 = bitcast %class.Array* %3 to i8*
  call void @_ZdlPv(i8* %6) #8
  br label %7

7:                                                ; preds = %5, %1
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr void @_ZN5ArrayD1Ev(%class.Array* %0) unnamed_addr  {
  %2 = alloca %class.Array*, align 8
  store %class.Array* %0, %class.Array** %2, align 8
  %3 = load %class.Array*, %class.Array** %2, align 8
  call void @_ZN5ArrayD2Ev(%class.Array* %3) #2
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define void @__quantum__rt__array_update_alias_count(%class.Array* %0, i32 %1)  {
  %3 = alloca %class.Array*, align 8
  %4 = alloca i32, align 4
  store %class.Array* %0, %class.Array** %3, align 8
  store i32 %1, i32* %4, align 4
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define void @__quantum__rt__array_update_reference_count(%class.Array* %0, i32 %1)  {
  %3 = alloca %class.Array*, align 8
  %4 = alloca i32, align 4
  store %class.Array* %0, %class.Array** %3, align 8
  store i32 %1, i32* %4, align 4
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define %class.Array* @__quantum__rt__array_copy(%class.Array* %0, i1 zeroext %1)  {
  %3 = alloca %class.Array*, align 8
  %4 = alloca %class.Array*, align 8
  %5 = alloca i8, align 1
  store %class.Array* %0, %class.Array** %4, align 8
  %6 = zext i1 %1 to i8
  store i8 %6, i8* %5, align 1
  %7 = load %class.Array*, %class.Array** %4, align 8
  %8 = icmp eq %class.Array* %7, null
  br i1 %8, label %9, label %10

9:                                                ; preds = %2
  store %class.Array* null, %class.Array** %3, align 8
  br label %12

10:                                               ; preds = %2
  %11 = load %class.Array*, %class.Array** %4, align 8
  store %class.Array* %11, %class.Array** %3, align 8
  br label %12

12:                                               ; preds = %10, %9
  %13 = load %class.Array*, %class.Array** %3, align 8
  ret %class.Array* %13
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZN5ArrayC2Exx(%class.Array* %0, i64 %1, i64 %2) unnamed_addr  {
  %4 = alloca %class.Array*, align 8
  %5 = alloca i64, align 8
  %6 = alloca i64, align 8
  store %class.Array* %0, %class.Array** %4, align 8
  store i64 %1, i64* %5, align 8
  store i64 %2, i64* %6, align 8
  %7 = load %class.Array*, %class.Array** %4, align 8
  %8 = getelementptr inbounds %class.Array, %class.Array* %7, i32 0, i32 0
  %9 = call nonnull align 8 dereferenceable(8) i64* @_ZNSt3__14moveIRxEEONS_16remove_referenceIT_E4typeEOS3_(i64* nonnull align 8 dereferenceable(8) %5) #2
  %10 = load i64, i64* %9, align 8
  store i64 %10, i64* %8, align 8
  %11 = getelementptr inbounds %class.Array, %class.Array* %7, i32 0, i32 1
  %12 = call nonnull align 8 dereferenceable(8) i64* @_ZNSt3__14moveIRxEEONS_16remove_referenceIT_E4typeEOS3_(i64* nonnull align 8 dereferenceable(8) %6) #2
  %13 = load i64, i64* %12, align 8
  store i64 %13, i64* %11, align 8
  %14 = getelementptr inbounds %class.Array, %class.Array* %7, i32 0, i32 0
  %15 = load i64, i64* %14, align 8
  %16 = getelementptr inbounds %class.Array, %class.Array* %7, i32 0, i32 1
  %17 = load i64, i64* %16, align 8
  %18 = mul nsw i64 %15, %17
  %19 = call noalias nonnull i8* @_Znam(i64 %18) #7
  %20 = getelementptr inbounds %class.Array, %class.Array* %7, i32 0, i32 2
  store i8* %19, i8** %20, align 8
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 8 dereferenceable(8) i64* @_ZNSt3__14moveIRxEEONS_16remove_referenceIT_E4typeEOS3_(i64* nonnull align 8 dereferenceable(8) %0)  {
  %2 = alloca i64*, align 8
  store i64* %0, i64** %2, align 8
  %3 = load i64*, i64** %2, align 8
  ret i64* %3
}

; Function Attrs: nobuiltin allocsize(0)
declare nonnull i8* @_Znam(i64) 

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr void @_ZN5ArrayD2Ev(%class.Array* %0) unnamed_addr  {
  %2 = alloca %class.Array*, align 8
  store %class.Array* %0, %class.Array** %2, align 8
  %3 = load %class.Array*, %class.Array** %2, align 8
  %4 = getelementptr inbounds %class.Array, %class.Array* %3, i32 0, i32 2
  %5 = load i8*, i8** %4, align 8
  %6 = icmp eq i8* %5, null
  br i1 %6, label %8, label %7

7:                                                ; preds = %1
  call void @_ZdaPv(i8* %5) #8
  br label %8

8:                                                ; preds = %7, %1
  ret void
}

; Function Attrs: nobuiltin nounwind
declare void @_ZdaPv(i8*) 

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__113unordered_mapIP5ArrayiNS_4hashIS2_EENS_8equal_toIS2_EENS_9allocatorINS_4pairIKS2_iEEEEEC2Ev(%"class.std::__1::unordered_map"* %0) unnamed_addr  {
  %2 = alloca %"class.std::__1::unordered_map"*, align 8
  store %"class.std::__1::unordered_map"* %0, %"class.std::__1::unordered_map"** %2, align 8
  %3 = load %"class.std::__1::unordered_map"*, %"class.std::__1::unordered_map"** %2, align 8
  %4 = getelementptr inbounds %"class.std::__1::unordered_map", %"class.std::__1::unordered_map"* %3, i32 0, i32 0
  call void @_ZNSt3__112__hash_tableINS_17__hash_value_typeIP5ArrayiEENS_22__unordered_map_hasherIS3_S4_NS_4hashIS3_EELb1EEENS_21__unordered_map_equalIS3_S4_NS_8equal_toIS3_EELb1EEENS_9allocatorIS4_EEEC1Ev(%"class.std::__1::__hash_table"* %4) #2
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__112__hash_tableINS_17__hash_value_typeIP5ArrayiEENS_22__unordered_map_hasherIS3_S4_NS_4hashIS3_EELb1EEENS_21__unordered_map_equalIS3_S4_NS_8equal_toIS3_EELb1EEENS_9allocatorIS4_EEEC1Ev(%"class.std::__1::__hash_table"* %0) unnamed_addr  {
  %2 = alloca %"class.std::__1::__hash_table"*, align 8
  store %"class.std::__1::__hash_table"* %0, %"class.std::__1::__hash_table"** %2, align 8
  %3 = load %"class.std::__1::__hash_table"*, %"class.std::__1::__hash_table"** %2, align 8
  call void @_ZNSt3__112__hash_tableINS_17__hash_value_typeIP5ArrayiEENS_22__unordered_map_hasherIS3_S4_NS_4hashIS3_EELb1EEENS_21__unordered_map_equalIS3_S4_NS_8equal_toIS3_EELb1EEENS_9allocatorIS4_EEEC2Ev(%"class.std::__1::__hash_table"* %3) #2
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__112__hash_tableINS_17__hash_value_typeIP5ArrayiEENS_22__unordered_map_hasherIS3_S4_NS_4hashIS3_EELb1EEENS_21__unordered_map_equalIS3_S4_NS_8equal_toIS3_EELb1EEENS_9allocatorIS4_EEEC2Ev(%"class.std::__1::__hash_table"* %0) unnamed_addr  personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*)  {
  %2 = alloca %"class.std::__1::__hash_table"*, align 8
  %3 = alloca i8*, align 8
  %4 = alloca i32, align 4
  %5 = alloca i32, align 4
  %6 = alloca %"struct.std::__1::__default_init_tag", align 1
  %7 = alloca float, align 4
  %8 = alloca %"struct.std::__1::__default_init_tag", align 1
  store %"class.std::__1::__hash_table"* %0, %"class.std::__1::__hash_table"** %2, align 8
  %9 = load %"class.std::__1::__hash_table"*, %"class.std::__1::__hash_table"** %2, align 8
  %10 = getelementptr inbounds %"class.std::__1::__hash_table", %"class.std::__1::__hash_table"* %9, i32 0, i32 0
  call void @_ZNSt3__110unique_ptrIA_PNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEENS_25__bucket_list_deallocatorINS_9allocatorISB_EEEEEC1ILb1EvEEv(%"class.std::__1::unique_ptr"* %10) #2
  %11 = getelementptr inbounds %"class.std::__1::__hash_table", %"class.std::__1::__hash_table"* %9, i32 0, i32 1
  invoke void @_ZNSt3__117__compressed_pairINS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEENS_9allocatorIS8_EEEC1ILb1EvEEv(%"class.std::__1::__compressed_pair.4"* %11)
          to label %12 unwind label %17

12:                                               ; preds = %1
  %13 = getelementptr inbounds %"class.std::__1::__hash_table", %"class.std::__1::__hash_table"* %9, i32 0, i32 2
  store i32 0, i32* %5, align 4
  invoke void @_ZNSt3__117__compressed_pairImNS_22__unordered_map_hasherIP5ArrayNS_17__hash_value_typeIS3_iEENS_4hashIS3_EELb1EEEEC1IiNS_18__default_init_tagEEEOT_OT0_(%"class.std::__1::__compressed_pair.9"* %13, i32* nonnull align 4 dereferenceable(4) %5, %"struct.std::__1::__default_init_tag"* nonnull align 1 dereferenceable(1) %6)
          to label %14 unwind label %17

14:                                               ; preds = %12
  %15 = getelementptr inbounds %"class.std::__1::__hash_table", %"class.std::__1::__hash_table"* %9, i32 0, i32 3
  store float 1.000000e+00, float* %7, align 4
  invoke void @_ZNSt3__117__compressed_pairIfNS_21__unordered_map_equalIP5ArrayNS_17__hash_value_typeIS3_iEENS_8equal_toIS3_EELb1EEEEC1IfNS_18__default_init_tagEEEOT_OT0_(%"class.std::__1::__compressed_pair.11"* %15, float* nonnull align 4 dereferenceable(4) %7, %"struct.std::__1::__default_init_tag"* nonnull align 1 dereferenceable(1) %8)
          to label %16 unwind label %17

16:                                               ; preds = %14
  ret void

17:                                               ; preds = %14, %12, %1
  %18 = landingpad { i8*, i32 }
          catch i8* null
  %19 = extractvalue { i8*, i32 } %18, 0
  store i8* %19, i8** %3, align 8
  %20 = extractvalue { i8*, i32 } %18, 1
  store i32 %20, i32* %4, align 4
  call void @_ZNSt3__110unique_ptrIA_PNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEENS_25__bucket_list_deallocatorINS_9allocatorISB_EEEEED1Ev(%"class.std::__1::unique_ptr"* %10) #2
  br label %21

21:                                               ; preds = %17
  %22 = load i8*, i8** %3, align 8
  call void @__clang_call_terminate(i8* %22) #9
  unreachable
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr void @_ZNSt3__110unique_ptrIA_PNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEENS_25__bucket_list_deallocatorINS_9allocatorISB_EEEEEC1ILb1EvEEv(%"class.std::__1::unique_ptr"* %0) unnamed_addr  {
  %2 = alloca %"class.std::__1::unique_ptr"*, align 8
  store %"class.std::__1::unique_ptr"* %0, %"class.std::__1::unique_ptr"** %2, align 8
  %3 = load %"class.std::__1::unique_ptr"*, %"class.std::__1::unique_ptr"** %2, align 8
  call void @_ZNSt3__110unique_ptrIA_PNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEENS_25__bucket_list_deallocatorINS_9allocatorISB_EEEEEC2ILb1EvEEv(%"class.std::__1::unique_ptr"* %3) #2
  ret void
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZNSt3__117__compressed_pairINS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEENS_9allocatorIS8_EEEC1ILb1EvEEv(%"class.std::__1::__compressed_pair.4"* %0) unnamed_addr  {
  %2 = alloca %"class.std::__1::__compressed_pair.4"*, align 8
  store %"class.std::__1::__compressed_pair.4"* %0, %"class.std::__1::__compressed_pair.4"** %2, align 8
  %3 = load %"class.std::__1::__compressed_pair.4"*, %"class.std::__1::__compressed_pair.4"** %2, align 8
  call void @_ZNSt3__117__compressed_pairINS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEENS_9allocatorIS8_EEEC2ILb1EvEEv(%"class.std::__1::__compressed_pair.4"* %3)
  ret void
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZNSt3__117__compressed_pairImNS_22__unordered_map_hasherIP5ArrayNS_17__hash_value_typeIS3_iEENS_4hashIS3_EELb1EEEEC1IiNS_18__default_init_tagEEEOT_OT0_(%"class.std::__1::__compressed_pair.9"* %0, i32* nonnull align 4 dereferenceable(4) %1, %"struct.std::__1::__default_init_tag"* nonnull align 1 dereferenceable(1) %2) unnamed_addr  {
  %4 = alloca %"class.std::__1::__compressed_pair.9"*, align 8
  %5 = alloca i32*, align 8
  %6 = alloca %"struct.std::__1::__default_init_tag"*, align 8
  store %"class.std::__1::__compressed_pair.9"* %0, %"class.std::__1::__compressed_pair.9"** %4, align 8
  store i32* %1, i32** %5, align 8
  store %"struct.std::__1::__default_init_tag"* %2, %"struct.std::__1::__default_init_tag"** %6, align 8
  %7 = load %"class.std::__1::__compressed_pair.9"*, %"class.std::__1::__compressed_pair.9"** %4, align 8
  %8 = load i32*, i32** %5, align 8
  %9 = load %"struct.std::__1::__default_init_tag"*, %"struct.std::__1::__default_init_tag"** %6, align 8
  call void @_ZNSt3__117__compressed_pairImNS_22__unordered_map_hasherIP5ArrayNS_17__hash_value_typeIS3_iEENS_4hashIS3_EELb1EEEEC2IiNS_18__default_init_tagEEEOT_OT0_(%"class.std::__1::__compressed_pair.9"* %7, i32* nonnull align 4 dereferenceable(4) %8, %"struct.std::__1::__default_init_tag"* nonnull align 1 dereferenceable(1) %9)
  ret void
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZNSt3__117__compressed_pairIfNS_21__unordered_map_equalIP5ArrayNS_17__hash_value_typeIS3_iEENS_8equal_toIS3_EELb1EEEEC1IfNS_18__default_init_tagEEEOT_OT0_(%"class.std::__1::__compressed_pair.11"* %0, float* nonnull align 4 dereferenceable(4) %1, %"struct.std::__1::__default_init_tag"* nonnull align 1 dereferenceable(1) %2) unnamed_addr  {
  %4 = alloca %"class.std::__1::__compressed_pair.11"*, align 8
  %5 = alloca float*, align 8
  %6 = alloca %"struct.std::__1::__default_init_tag"*, align 8
  store %"class.std::__1::__compressed_pair.11"* %0, %"class.std::__1::__compressed_pair.11"** %4, align 8
  store float* %1, float** %5, align 8
  store %"struct.std::__1::__default_init_tag"* %2, %"struct.std::__1::__default_init_tag"** %6, align 8
  %7 = load %"class.std::__1::__compressed_pair.11"*, %"class.std::__1::__compressed_pair.11"** %4, align 8
  %8 = load float*, float** %5, align 8
  %9 = load %"struct.std::__1::__default_init_tag"*, %"struct.std::__1::__default_init_tag"** %6, align 8
  call void @_ZNSt3__117__compressed_pairIfNS_21__unordered_map_equalIP5ArrayNS_17__hash_value_typeIS3_iEENS_8equal_toIS3_EELb1EEEEC2IfNS_18__default_init_tagEEEOT_OT0_(%"class.std::__1::__compressed_pair.11"* %7, float* nonnull align 4 dereferenceable(4) %8, %"struct.std::__1::__default_init_tag"* nonnull align 1 dereferenceable(1) %9)
  ret void
}

; Function Attrs: noinline noreturn nounwind
define linkonce_odr hidden void @__clang_call_terminate(i8* %0)  {
  %2 = call i8* @__cxa_begin_catch(i8* %0) #2
  call void @_ZSt9terminatev() #9
  unreachable
}

declare i8* @__cxa_begin_catch(i8*)

declare void @_ZSt9terminatev()

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__110unique_ptrIA_PNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEENS_25__bucket_list_deallocatorINS_9allocatorISB_EEEEED1Ev(%"class.std::__1::unique_ptr"* %0) unnamed_addr  {
  %2 = alloca %"class.std::__1::unique_ptr"*, align 8
  store %"class.std::__1::unique_ptr"* %0, %"class.std::__1::unique_ptr"** %2, align 8
  %3 = load %"class.std::__1::unique_ptr"*, %"class.std::__1::unique_ptr"** %2, align 8
  call void @_ZNSt3__110unique_ptrIA_PNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEENS_25__bucket_list_deallocatorINS_9allocatorISB_EEEEED2Ev(%"class.std::__1::unique_ptr"* %3) #2
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr void @_ZNSt3__110unique_ptrIA_PNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEENS_25__bucket_list_deallocatorINS_9allocatorISB_EEEEEC2ILb1EvEEv(%"class.std::__1::unique_ptr"* %0) unnamed_addr  personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*)  {
  %2 = alloca %"class.std::__1::unique_ptr"*, align 8
  %3 = alloca %"struct.std::__1::__hash_node_base"**, align 8
  %4 = alloca %"struct.std::__1::__default_init_tag", align 1
  store %"class.std::__1::unique_ptr"* %0, %"class.std::__1::unique_ptr"** %2, align 8
  %5 = load %"class.std::__1::unique_ptr"*, %"class.std::__1::unique_ptr"** %2, align 8
  %6 = getelementptr inbounds %"class.std::__1::unique_ptr", %"class.std::__1::unique_ptr"* %5, i32 0, i32 0
  store %"struct.std::__1::__hash_node_base"** null, %"struct.std::__1::__hash_node_base"*** %3, align 8
  invoke void @_ZNSt3__117__compressed_pairIPPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEENS_25__bucket_list_deallocatorINS_9allocatorISB_EEEEEC1ISC_NS_18__default_init_tagEEEOT_OT0_(%"class.std::__1::__compressed_pair"* %6, %"struct.std::__1::__hash_node_base"*** nonnull align 8 dereferenceable(8) %3, %"struct.std::__1::__default_init_tag"* nonnull align 1 dereferenceable(1) %4)
          to label %7 unwind label %8

7:                                                ; preds = %1
  ret void

8:                                                ; preds = %1
  %9 = landingpad { i8*, i32 }
          catch i8* null
  %10 = extractvalue { i8*, i32 } %9, 0
  call void @__clang_call_terminate(i8* %10) #9
  unreachable
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZNSt3__117__compressed_pairIPPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEENS_25__bucket_list_deallocatorINS_9allocatorISB_EEEEEC1ISC_NS_18__default_init_tagEEEOT_OT0_(%"class.std::__1::__compressed_pair"* %0, %"struct.std::__1::__hash_node_base"*** nonnull align 8 dereferenceable(8) %1, %"struct.std::__1::__default_init_tag"* nonnull align 1 dereferenceable(1) %2) unnamed_addr  {
  %4 = alloca %"class.std::__1::__compressed_pair"*, align 8
  %5 = alloca %"struct.std::__1::__hash_node_base"***, align 8
  %6 = alloca %"struct.std::__1::__default_init_tag"*, align 8
  store %"class.std::__1::__compressed_pair"* %0, %"class.std::__1::__compressed_pair"** %4, align 8
  store %"struct.std::__1::__hash_node_base"*** %1, %"struct.std::__1::__hash_node_base"**** %5, align 8
  store %"struct.std::__1::__default_init_tag"* %2, %"struct.std::__1::__default_init_tag"** %6, align 8
  %7 = load %"class.std::__1::__compressed_pair"*, %"class.std::__1::__compressed_pair"** %4, align 8
  %8 = load %"struct.std::__1::__hash_node_base"***, %"struct.std::__1::__hash_node_base"**** %5, align 8
  %9 = load %"struct.std::__1::__default_init_tag"*, %"struct.std::__1::__default_init_tag"** %6, align 8
  call void @_ZNSt3__117__compressed_pairIPPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEENS_25__bucket_list_deallocatorINS_9allocatorISB_EEEEEC2ISC_NS_18__default_init_tagEEEOT_OT0_(%"class.std::__1::__compressed_pair"* %7, %"struct.std::__1::__hash_node_base"*** nonnull align 8 dereferenceable(8) %8, %"struct.std::__1::__default_init_tag"* nonnull align 1 dereferenceable(1) %9)
  ret void
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZNSt3__117__compressed_pairIPPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEENS_25__bucket_list_deallocatorINS_9allocatorISB_EEEEEC2ISC_NS_18__default_init_tagEEEOT_OT0_(%"class.std::__1::__compressed_pair"* %0, %"struct.std::__1::__hash_node_base"*** nonnull align 8 dereferenceable(8) %1, %"struct.std::__1::__default_init_tag"* nonnull align 1 dereferenceable(1) %2) unnamed_addr  {
  %4 = alloca %"class.std::__1::__compressed_pair"*, align 8
  %5 = alloca %"struct.std::__1::__hash_node_base"***, align 8
  %6 = alloca %"struct.std::__1::__default_init_tag"*, align 8
  %7 = alloca %"struct.std::__1::__default_init_tag", align 1
  store %"class.std::__1::__compressed_pair"* %0, %"class.std::__1::__compressed_pair"** %4, align 8
  store %"struct.std::__1::__hash_node_base"*** %1, %"struct.std::__1::__hash_node_base"**** %5, align 8
  store %"struct.std::__1::__default_init_tag"* %2, %"struct.std::__1::__default_init_tag"** %6, align 8
  %8 = load %"class.std::__1::__compressed_pair"*, %"class.std::__1::__compressed_pair"** %4, align 8
  %9 = bitcast %"class.std::__1::__compressed_pair"* %8 to %"struct.std::__1::__compressed_pair_elem"*
  %10 = load %"struct.std::__1::__hash_node_base"***, %"struct.std::__1::__hash_node_base"**** %5, align 8
  %11 = call nonnull align 8 dereferenceable(8) %"struct.std::__1::__hash_node_base"*** @_ZNSt3__17forwardIPPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEEEOT_RNS_16remove_referenceISD_E4typeE(%"struct.std::__1::__hash_node_base"*** nonnull align 8 dereferenceable(8) %10) #2
  call void @_ZNSt3__122__compressed_pair_elemIPPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEELi0ELb0EEC2ISC_vEEOT_(%"struct.std::__1::__compressed_pair_elem"* %9, %"struct.std::__1::__hash_node_base"*** nonnull align 8 dereferenceable(8) %11)
  %12 = bitcast %"class.std::__1::__compressed_pair"* %8 to i8*
  %13 = getelementptr inbounds i8, i8* %12, i64 8
  %14 = bitcast i8* %13 to %"struct.std::__1::__compressed_pair_elem.0"*
  %15 = load %"struct.std::__1::__default_init_tag"*, %"struct.std::__1::__default_init_tag"** %6, align 8
  %16 = call nonnull align 1 dereferenceable(1) %"struct.std::__1::__default_init_tag"* @_ZNSt3__17forwardINS_18__default_init_tagEEEOT_RNS_16remove_referenceIS2_E4typeE(%"struct.std::__1::__default_init_tag"* nonnull align 1 dereferenceable(1) %15) #2
  call void @_ZNSt3__122__compressed_pair_elemINS_25__bucket_list_deallocatorINS_9allocatorIPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEEEEELi1ELb0EEC2ENS_18__default_init_tagE(%"struct.std::__1::__compressed_pair_elem.0"* %14)
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 8 dereferenceable(8) %"struct.std::__1::__hash_node_base"*** @_ZNSt3__17forwardIPPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEEEOT_RNS_16remove_referenceISD_E4typeE(%"struct.std::__1::__hash_node_base"*** nonnull align 8 dereferenceable(8) %0)  {
  %2 = alloca %"struct.std::__1::__hash_node_base"***, align 8
  store %"struct.std::__1::__hash_node_base"*** %0, %"struct.std::__1::__hash_node_base"**** %2, align 8
  %3 = load %"struct.std::__1::__hash_node_base"***, %"struct.std::__1::__hash_node_base"**** %2, align 8
  ret %"struct.std::__1::__hash_node_base"*** %3
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr void @_ZNSt3__122__compressed_pair_elemIPPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEELi0ELb0EEC2ISC_vEEOT_(%"struct.std::__1::__compressed_pair_elem"* %0, %"struct.std::__1::__hash_node_base"*** nonnull align 8 dereferenceable(8) %1) unnamed_addr  {
  %3 = alloca %"struct.std::__1::__compressed_pair_elem"*, align 8
  %4 = alloca %"struct.std::__1::__hash_node_base"***, align 8
  store %"struct.std::__1::__compressed_pair_elem"* %0, %"struct.std::__1::__compressed_pair_elem"** %3, align 8
  store %"struct.std::__1::__hash_node_base"*** %1, %"struct.std::__1::__hash_node_base"**** %4, align 8
  %5 = load %"struct.std::__1::__compressed_pair_elem"*, %"struct.std::__1::__compressed_pair_elem"** %3, align 8
  %6 = getelementptr inbounds %"struct.std::__1::__compressed_pair_elem", %"struct.std::__1::__compressed_pair_elem"* %5, i32 0, i32 0
  %7 = load %"struct.std::__1::__hash_node_base"***, %"struct.std::__1::__hash_node_base"**** %4, align 8
  %8 = call nonnull align 8 dereferenceable(8) %"struct.std::__1::__hash_node_base"*** @_ZNSt3__17forwardIPPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEEEOT_RNS_16remove_referenceISD_E4typeE(%"struct.std::__1::__hash_node_base"*** nonnull align 8 dereferenceable(8) %7) #2
  %9 = load %"struct.std::__1::__hash_node_base"**, %"struct.std::__1::__hash_node_base"*** %8, align 8
  store %"struct.std::__1::__hash_node_base"** %9, %"struct.std::__1::__hash_node_base"*** %6, align 8
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 1 dereferenceable(1) %"struct.std::__1::__default_init_tag"* @_ZNSt3__17forwardINS_18__default_init_tagEEEOT_RNS_16remove_referenceIS2_E4typeE(%"struct.std::__1::__default_init_tag"* nonnull align 1 dereferenceable(1) %0)  {
  %2 = alloca %"struct.std::__1::__default_init_tag"*, align 8
  store %"struct.std::__1::__default_init_tag"* %0, %"struct.std::__1::__default_init_tag"** %2, align 8
  %3 = load %"struct.std::__1::__default_init_tag"*, %"struct.std::__1::__default_init_tag"** %2, align 8
  ret %"struct.std::__1::__default_init_tag"* %3
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__122__compressed_pair_elemINS_25__bucket_list_deallocatorINS_9allocatorIPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEEEEELi1ELb0EEC2ENS_18__default_init_tagE(%"struct.std::__1::__compressed_pair_elem.0"* %0) unnamed_addr  {
  %2 = alloca %"struct.std::__1::__default_init_tag", align 1
  %3 = alloca %"struct.std::__1::__compressed_pair_elem.0"*, align 8
  store %"struct.std::__1::__compressed_pair_elem.0"* %0, %"struct.std::__1::__compressed_pair_elem.0"** %3, align 8
  %4 = load %"struct.std::__1::__compressed_pair_elem.0"*, %"struct.std::__1::__compressed_pair_elem.0"** %3, align 8
  %5 = getelementptr inbounds %"struct.std::__1::__compressed_pair_elem.0", %"struct.std::__1::__compressed_pair_elem.0"* %4, i32 0, i32 0
  call void @_ZNSt3__125__bucket_list_deallocatorINS_9allocatorIPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEEEEC1Ev(%"class.std::__1::__bucket_list_deallocator"* %5) #2
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__125__bucket_list_deallocatorINS_9allocatorIPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEEEEC1Ev(%"class.std::__1::__bucket_list_deallocator"* %0) unnamed_addr  {
  %2 = alloca %"class.std::__1::__bucket_list_deallocator"*, align 8
  store %"class.std::__1::__bucket_list_deallocator"* %0, %"class.std::__1::__bucket_list_deallocator"** %2, align 8
  %3 = load %"class.std::__1::__bucket_list_deallocator"*, %"class.std::__1::__bucket_list_deallocator"** %2, align 8
  call void @_ZNSt3__125__bucket_list_deallocatorINS_9allocatorIPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEEEEC2Ev(%"class.std::__1::__bucket_list_deallocator"* %3) #2
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__125__bucket_list_deallocatorINS_9allocatorIPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEEEEC2Ev(%"class.std::__1::__bucket_list_deallocator"* %0) unnamed_addr  personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*)  {
  %2 = alloca %"class.std::__1::__bucket_list_deallocator"*, align 8
  %3 = alloca i32, align 4
  %4 = alloca %"struct.std::__1::__default_init_tag", align 1
  store %"class.std::__1::__bucket_list_deallocator"* %0, %"class.std::__1::__bucket_list_deallocator"** %2, align 8
  %5 = load %"class.std::__1::__bucket_list_deallocator"*, %"class.std::__1::__bucket_list_deallocator"** %2, align 8
  %6 = getelementptr inbounds %"class.std::__1::__bucket_list_deallocator", %"class.std::__1::__bucket_list_deallocator"* %5, i32 0, i32 0
  store i32 0, i32* %3, align 4
  invoke void @_ZNSt3__117__compressed_pairImNS_9allocatorIPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEEEEC1IiNS_18__default_init_tagEEEOT_OT0_(%"class.std::__1::__compressed_pair.1"* %6, i32* nonnull align 4 dereferenceable(4) %3, %"struct.std::__1::__default_init_tag"* nonnull align 1 dereferenceable(1) %4)
          to label %7 unwind label %8

7:                                                ; preds = %1
  ret void

8:                                                ; preds = %1
  %9 = landingpad { i8*, i32 }
          catch i8* null
  %10 = extractvalue { i8*, i32 } %9, 0
  call void @__clang_call_terminate(i8* %10) #9
  unreachable
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZNSt3__117__compressed_pairImNS_9allocatorIPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEEEEC1IiNS_18__default_init_tagEEEOT_OT0_(%"class.std::__1::__compressed_pair.1"* %0, i32* nonnull align 4 dereferenceable(4) %1, %"struct.std::__1::__default_init_tag"* nonnull align 1 dereferenceable(1) %2) unnamed_addr  {
  %4 = alloca %"class.std::__1::__compressed_pair.1"*, align 8
  %5 = alloca i32*, align 8
  %6 = alloca %"struct.std::__1::__default_init_tag"*, align 8
  store %"class.std::__1::__compressed_pair.1"* %0, %"class.std::__1::__compressed_pair.1"** %4, align 8
  store i32* %1, i32** %5, align 8
  store %"struct.std::__1::__default_init_tag"* %2, %"struct.std::__1::__default_init_tag"** %6, align 8
  %7 = load %"class.std::__1::__compressed_pair.1"*, %"class.std::__1::__compressed_pair.1"** %4, align 8
  %8 = load i32*, i32** %5, align 8
  %9 = load %"struct.std::__1::__default_init_tag"*, %"struct.std::__1::__default_init_tag"** %6, align 8
  call void @_ZNSt3__117__compressed_pairImNS_9allocatorIPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEEEEC2IiNS_18__default_init_tagEEEOT_OT0_(%"class.std::__1::__compressed_pair.1"* %7, i32* nonnull align 4 dereferenceable(4) %8, %"struct.std::__1::__default_init_tag"* nonnull align 1 dereferenceable(1) %9)
  ret void
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZNSt3__117__compressed_pairImNS_9allocatorIPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEEEEC2IiNS_18__default_init_tagEEEOT_OT0_(%"class.std::__1::__compressed_pair.1"* %0, i32* nonnull align 4 dereferenceable(4) %1, %"struct.std::__1::__default_init_tag"* nonnull align 1 dereferenceable(1) %2) unnamed_addr  {
  %4 = alloca %"class.std::__1::__compressed_pair.1"*, align 8
  %5 = alloca i32*, align 8
  %6 = alloca %"struct.std::__1::__default_init_tag"*, align 8
  %7 = alloca %"struct.std::__1::__default_init_tag", align 1
  store %"class.std::__1::__compressed_pair.1"* %0, %"class.std::__1::__compressed_pair.1"** %4, align 8
  store i32* %1, i32** %5, align 8
  store %"struct.std::__1::__default_init_tag"* %2, %"struct.std::__1::__default_init_tag"** %6, align 8
  %8 = load %"class.std::__1::__compressed_pair.1"*, %"class.std::__1::__compressed_pair.1"** %4, align 8
  %9 = bitcast %"class.std::__1::__compressed_pair.1"* %8 to %"struct.std::__1::__compressed_pair_elem.2"*
  %10 = load i32*, i32** %5, align 8
  %11 = call nonnull align 4 dereferenceable(4) i32* @_ZNSt3__17forwardIiEEOT_RNS_16remove_referenceIS1_E4typeE(i32* nonnull align 4 dereferenceable(4) %10) #2
  call void @_ZNSt3__122__compressed_pair_elemImLi0ELb0EEC2IivEEOT_(%"struct.std::__1::__compressed_pair_elem.2"* %9, i32* nonnull align 4 dereferenceable(4) %11)
  %12 = bitcast %"class.std::__1::__compressed_pair.1"* %8 to %"struct.std::__1::__compressed_pair_elem.3"*
  %13 = load %"struct.std::__1::__default_init_tag"*, %"struct.std::__1::__default_init_tag"** %6, align 8
  %14 = call nonnull align 1 dereferenceable(1) %"struct.std::__1::__default_init_tag"* @_ZNSt3__17forwardINS_18__default_init_tagEEEOT_RNS_16remove_referenceIS2_E4typeE(%"struct.std::__1::__default_init_tag"* nonnull align 1 dereferenceable(1) %13) #2
  call void @_ZNSt3__122__compressed_pair_elemINS_9allocatorIPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEEELi1ELb1EEC2ENS_18__default_init_tagE(%"struct.std::__1::__compressed_pair_elem.3"* %12)
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 4 dereferenceable(4) i32* @_ZNSt3__17forwardIiEEOT_RNS_16remove_referenceIS1_E4typeE(i32* nonnull align 4 dereferenceable(4) %0)  {
  %2 = alloca i32*, align 8
  store i32* %0, i32** %2, align 8
  %3 = load i32*, i32** %2, align 8
  ret i32* %3
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr void @_ZNSt3__122__compressed_pair_elemImLi0ELb0EEC2IivEEOT_(%"struct.std::__1::__compressed_pair_elem.2"* %0, i32* nonnull align 4 dereferenceable(4) %1) unnamed_addr  {
  %3 = alloca %"struct.std::__1::__compressed_pair_elem.2"*, align 8
  %4 = alloca i32*, align 8
  store %"struct.std::__1::__compressed_pair_elem.2"* %0, %"struct.std::__1::__compressed_pair_elem.2"** %3, align 8
  store i32* %1, i32** %4, align 8
  %5 = load %"struct.std::__1::__compressed_pair_elem.2"*, %"struct.std::__1::__compressed_pair_elem.2"** %3, align 8
  %6 = getelementptr inbounds %"struct.std::__1::__compressed_pair_elem.2", %"struct.std::__1::__compressed_pair_elem.2"* %5, i32 0, i32 0
  %7 = load i32*, i32** %4, align 8
  %8 = call nonnull align 4 dereferenceable(4) i32* @_ZNSt3__17forwardIiEEOT_RNS_16remove_referenceIS1_E4typeE(i32* nonnull align 4 dereferenceable(4) %7) #2
  %9 = load i32, i32* %8, align 4
  %10 = sext i32 %9 to i64
  store i64 %10, i64* %6, align 8
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__122__compressed_pair_elemINS_9allocatorIPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEEELi1ELb1EEC2ENS_18__default_init_tagE(%"struct.std::__1::__compressed_pair_elem.3"* %0) unnamed_addr  {
  %2 = alloca %"struct.std::__1::__default_init_tag", align 1
  %3 = alloca %"struct.std::__1::__compressed_pair_elem.3"*, align 8
  store %"struct.std::__1::__compressed_pair_elem.3"* %0, %"struct.std::__1::__compressed_pair_elem.3"** %3, align 8
  %4 = load %"struct.std::__1::__compressed_pair_elem.3"*, %"struct.std::__1::__compressed_pair_elem.3"** %3, align 8
  %5 = bitcast %"struct.std::__1::__compressed_pair_elem.3"* %4 to %"class.std::__1::allocator"*
  call void @_ZNSt3__19allocatorIPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEEC2Ev(%"class.std::__1::allocator"* %5) #2
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__19allocatorIPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEEC2Ev(%"class.std::__1::allocator"* %0) unnamed_addr  {
  %2 = alloca %"class.std::__1::allocator"*, align 8
  store %"class.std::__1::allocator"* %0, %"class.std::__1::allocator"** %2, align 8
  %3 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %2, align 8
  ret void
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZNSt3__117__compressed_pairINS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEENS_9allocatorIS8_EEEC2ILb1EvEEv(%"class.std::__1::__compressed_pair.4"* %0) unnamed_addr  {
  %2 = alloca %"class.std::__1::__compressed_pair.4"*, align 8
  %3 = alloca %"struct.std::__1::__value_init_tag", align 1
  %4 = alloca %"struct.std::__1::__value_init_tag", align 1
  store %"class.std::__1::__compressed_pair.4"* %0, %"class.std::__1::__compressed_pair.4"** %2, align 8
  %5 = load %"class.std::__1::__compressed_pair.4"*, %"class.std::__1::__compressed_pair.4"** %2, align 8
  %6 = bitcast %"class.std::__1::__compressed_pair.4"* %5 to %"struct.std::__1::__compressed_pair_elem.5"*
  call void @_ZNSt3__122__compressed_pair_elemINS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEELi0ELb0EEC2ENS_16__value_init_tagE(%"struct.std::__1::__compressed_pair_elem.5"* %6)
  %7 = bitcast %"class.std::__1::__compressed_pair.4"* %5 to %"struct.std::__1::__compressed_pair_elem.6"*
  call void @_ZNSt3__122__compressed_pair_elemINS_9allocatorINS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEELi1ELb1EEC2ENS_16__value_init_tagE(%"struct.std::__1::__compressed_pair_elem.6"* %7)
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__122__compressed_pair_elemINS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEELi0ELb0EEC2ENS_16__value_init_tagE(%"struct.std::__1::__compressed_pair_elem.5"* %0) unnamed_addr  {
  %2 = alloca %"struct.std::__1::__value_init_tag", align 1
  %3 = alloca %"struct.std::__1::__compressed_pair_elem.5"*, align 8
  store %"struct.std::__1::__compressed_pair_elem.5"* %0, %"struct.std::__1::__compressed_pair_elem.5"** %3, align 8
  %4 = load %"struct.std::__1::__compressed_pair_elem.5"*, %"struct.std::__1::__compressed_pair_elem.5"** %3, align 8
  %5 = getelementptr inbounds %"struct.std::__1::__compressed_pair_elem.5", %"struct.std::__1::__compressed_pair_elem.5"* %4, i32 0, i32 0
  call void @_ZNSt3__116__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEC1Ev(%"struct.std::__1::__hash_node_base"* %5) #2
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__122__compressed_pair_elemINS_9allocatorINS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEELi1ELb1EEC2ENS_16__value_init_tagE(%"struct.std::__1::__compressed_pair_elem.6"* %0) unnamed_addr  {
  %2 = alloca %"struct.std::__1::__value_init_tag", align 1
  %3 = alloca %"struct.std::__1::__compressed_pair_elem.6"*, align 8
  store %"struct.std::__1::__compressed_pair_elem.6"* %0, %"struct.std::__1::__compressed_pair_elem.6"** %3, align 8
  %4 = load %"struct.std::__1::__compressed_pair_elem.6"*, %"struct.std::__1::__compressed_pair_elem.6"** %3, align 8
  %5 = bitcast %"struct.std::__1::__compressed_pair_elem.6"* %4 to %"class.std::__1::allocator.7"*
  call void @_ZNSt3__19allocatorINS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEC2Ev(%"class.std::__1::allocator.7"* %5) #2
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__116__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEC1Ev(%"struct.std::__1::__hash_node_base"* %0) unnamed_addr  {
  %2 = alloca %"struct.std::__1::__hash_node_base"*, align 8
  store %"struct.std::__1::__hash_node_base"* %0, %"struct.std::__1::__hash_node_base"** %2, align 8
  %3 = load %"struct.std::__1::__hash_node_base"*, %"struct.std::__1::__hash_node_base"** %2, align 8
  call void @_ZNSt3__116__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEC2Ev(%"struct.std::__1::__hash_node_base"* %3) #2
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__116__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEC2Ev(%"struct.std::__1::__hash_node_base"* %0) unnamed_addr  {
  %2 = alloca %"struct.std::__1::__hash_node_base"*, align 8
  store %"struct.std::__1::__hash_node_base"* %0, %"struct.std::__1::__hash_node_base"** %2, align 8
  %3 = load %"struct.std::__1::__hash_node_base"*, %"struct.std::__1::__hash_node_base"** %2, align 8
  %4 = getelementptr inbounds %"struct.std::__1::__hash_node_base", %"struct.std::__1::__hash_node_base"* %3, i32 0, i32 0
  store %"struct.std::__1::__hash_node_base"* null, %"struct.std::__1::__hash_node_base"** %4, align 8
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__19allocatorINS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEC2Ev(%"class.std::__1::allocator.7"* %0) unnamed_addr  {
  %2 = alloca %"class.std::__1::allocator.7"*, align 8
  store %"class.std::__1::allocator.7"* %0, %"class.std::__1::allocator.7"** %2, align 8
  %3 = load %"class.std::__1::allocator.7"*, %"class.std::__1::allocator.7"** %2, align 8
  ret void
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZNSt3__117__compressed_pairImNS_22__unordered_map_hasherIP5ArrayNS_17__hash_value_typeIS3_iEENS_4hashIS3_EELb1EEEEC2IiNS_18__default_init_tagEEEOT_OT0_(%"class.std::__1::__compressed_pair.9"* %0, i32* nonnull align 4 dereferenceable(4) %1, %"struct.std::__1::__default_init_tag"* nonnull align 1 dereferenceable(1) %2) unnamed_addr  {
  %4 = alloca %"class.std::__1::__compressed_pair.9"*, align 8
  %5 = alloca i32*, align 8
  %6 = alloca %"struct.std::__1::__default_init_tag"*, align 8
  %7 = alloca %"struct.std::__1::__default_init_tag", align 1
  store %"class.std::__1::__compressed_pair.9"* %0, %"class.std::__1::__compressed_pair.9"** %4, align 8
  store i32* %1, i32** %5, align 8
  store %"struct.std::__1::__default_init_tag"* %2, %"struct.std::__1::__default_init_tag"** %6, align 8
  %8 = load %"class.std::__1::__compressed_pair.9"*, %"class.std::__1::__compressed_pair.9"** %4, align 8
  %9 = bitcast %"class.std::__1::__compressed_pair.9"* %8 to %"struct.std::__1::__compressed_pair_elem.2"*
  %10 = load i32*, i32** %5, align 8
  %11 = call nonnull align 4 dereferenceable(4) i32* @_ZNSt3__17forwardIiEEOT_RNS_16remove_referenceIS1_E4typeE(i32* nonnull align 4 dereferenceable(4) %10) #2
  call void @_ZNSt3__122__compressed_pair_elemImLi0ELb0EEC2IivEEOT_(%"struct.std::__1::__compressed_pair_elem.2"* %9, i32* nonnull align 4 dereferenceable(4) %11)
  %12 = bitcast %"class.std::__1::__compressed_pair.9"* %8 to %"struct.std::__1::__compressed_pair_elem.10"*
  %13 = load %"struct.std::__1::__default_init_tag"*, %"struct.std::__1::__default_init_tag"** %6, align 8
  %14 = call nonnull align 1 dereferenceable(1) %"struct.std::__1::__default_init_tag"* @_ZNSt3__17forwardINS_18__default_init_tagEEEOT_RNS_16remove_referenceIS2_E4typeE(%"struct.std::__1::__default_init_tag"* nonnull align 1 dereferenceable(1) %13) #2
  call void @_ZNSt3__122__compressed_pair_elemINS_22__unordered_map_hasherIP5ArrayNS_17__hash_value_typeIS3_iEENS_4hashIS3_EELb1EEELi1ELb1EEC2ENS_18__default_init_tagE(%"struct.std::__1::__compressed_pair_elem.10"* %12)
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__122__compressed_pair_elemINS_22__unordered_map_hasherIP5ArrayNS_17__hash_value_typeIS3_iEENS_4hashIS3_EELb1EEELi1ELb1EEC2ENS_18__default_init_tagE(%"struct.std::__1::__compressed_pair_elem.10"* %0) unnamed_addr  {
  %2 = alloca %"struct.std::__1::__default_init_tag", align 1
  %3 = alloca %"struct.std::__1::__compressed_pair_elem.10"*, align 8
  store %"struct.std::__1::__compressed_pair_elem.10"* %0, %"struct.std::__1::__compressed_pair_elem.10"** %3, align 8
  %4 = load %"struct.std::__1::__compressed_pair_elem.10"*, %"struct.std::__1::__compressed_pair_elem.10"** %3, align 8
  %5 = bitcast %"struct.std::__1::__compressed_pair_elem.10"* %4 to %"class.std::__1::__unordered_map_hasher"*
  call void @_ZNSt3__122__unordered_map_hasherIP5ArrayNS_17__hash_value_typeIS2_iEENS_4hashIS2_EELb1EEC2Ev(%"class.std::__1::__unordered_map_hasher"* %5) #2
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__122__unordered_map_hasherIP5ArrayNS_17__hash_value_typeIS2_iEENS_4hashIS2_EELb1EEC2Ev(%"class.std::__1::__unordered_map_hasher"* %0) unnamed_addr  {
  %2 = alloca %"class.std::__1::__unordered_map_hasher"*, align 8
  store %"class.std::__1::__unordered_map_hasher"* %0, %"class.std::__1::__unordered_map_hasher"** %2, align 8
  %3 = load %"class.std::__1::__unordered_map_hasher"*, %"class.std::__1::__unordered_map_hasher"** %2, align 8
  %4 = bitcast %"class.std::__1::__unordered_map_hasher"* %3 to %"struct.std::__1::hash"*
  ret void
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZNSt3__117__compressed_pairIfNS_21__unordered_map_equalIP5ArrayNS_17__hash_value_typeIS3_iEENS_8equal_toIS3_EELb1EEEEC2IfNS_18__default_init_tagEEEOT_OT0_(%"class.std::__1::__compressed_pair.11"* %0, float* nonnull align 4 dereferenceable(4) %1, %"struct.std::__1::__default_init_tag"* nonnull align 1 dereferenceable(1) %2) unnamed_addr  {
  %4 = alloca %"class.std::__1::__compressed_pair.11"*, align 8
  %5 = alloca float*, align 8
  %6 = alloca %"struct.std::__1::__default_init_tag"*, align 8
  %7 = alloca %"struct.std::__1::__default_init_tag", align 1
  store %"class.std::__1::__compressed_pair.11"* %0, %"class.std::__1::__compressed_pair.11"** %4, align 8
  store float* %1, float** %5, align 8
  store %"struct.std::__1::__default_init_tag"* %2, %"struct.std::__1::__default_init_tag"** %6, align 8
  %8 = load %"class.std::__1::__compressed_pair.11"*, %"class.std::__1::__compressed_pair.11"** %4, align 8
  %9 = bitcast %"class.std::__1::__compressed_pair.11"* %8 to %"struct.std::__1::__compressed_pair_elem.12"*
  %10 = load float*, float** %5, align 8
  %11 = call nonnull align 4 dereferenceable(4) float* @_ZNSt3__17forwardIfEEOT_RNS_16remove_referenceIS1_E4typeE(float* nonnull align 4 dereferenceable(4) %10) #2
  call void @_ZNSt3__122__compressed_pair_elemIfLi0ELb0EEC2IfvEEOT_(%"struct.std::__1::__compressed_pair_elem.12"* %9, float* nonnull align 4 dereferenceable(4) %11)
  %12 = bitcast %"class.std::__1::__compressed_pair.11"* %8 to %"struct.std::__1::__compressed_pair_elem.13"*
  %13 = load %"struct.std::__1::__default_init_tag"*, %"struct.std::__1::__default_init_tag"** %6, align 8
  %14 = call nonnull align 1 dereferenceable(1) %"struct.std::__1::__default_init_tag"* @_ZNSt3__17forwardINS_18__default_init_tagEEEOT_RNS_16remove_referenceIS2_E4typeE(%"struct.std::__1::__default_init_tag"* nonnull align 1 dereferenceable(1) %13) #2
  call void @_ZNSt3__122__compressed_pair_elemINS_21__unordered_map_equalIP5ArrayNS_17__hash_value_typeIS3_iEENS_8equal_toIS3_EELb1EEELi1ELb1EEC2ENS_18__default_init_tagE(%"struct.std::__1::__compressed_pair_elem.13"* %12)
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 4 dereferenceable(4) float* @_ZNSt3__17forwardIfEEOT_RNS_16remove_referenceIS1_E4typeE(float* nonnull align 4 dereferenceable(4) %0)  {
  %2 = alloca float*, align 8
  store float* %0, float** %2, align 8
  %3 = load float*, float** %2, align 8
  ret float* %3
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr void @_ZNSt3__122__compressed_pair_elemIfLi0ELb0EEC2IfvEEOT_(%"struct.std::__1::__compressed_pair_elem.12"* %0, float* nonnull align 4 dereferenceable(4) %1) unnamed_addr  {
  %3 = alloca %"struct.std::__1::__compressed_pair_elem.12"*, align 8
  %4 = alloca float*, align 8
  store %"struct.std::__1::__compressed_pair_elem.12"* %0, %"struct.std::__1::__compressed_pair_elem.12"** %3, align 8
  store float* %1, float** %4, align 8
  %5 = load %"struct.std::__1::__compressed_pair_elem.12"*, %"struct.std::__1::__compressed_pair_elem.12"** %3, align 8
  %6 = getelementptr inbounds %"struct.std::__1::__compressed_pair_elem.12", %"struct.std::__1::__compressed_pair_elem.12"* %5, i32 0, i32 0
  %7 = load float*, float** %4, align 8
  %8 = call nonnull align 4 dereferenceable(4) float* @_ZNSt3__17forwardIfEEOT_RNS_16remove_referenceIS1_E4typeE(float* nonnull align 4 dereferenceable(4) %7) #2
  %9 = load float, float* %8, align 4
  store float %9, float* %6, align 4
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__122__compressed_pair_elemINS_21__unordered_map_equalIP5ArrayNS_17__hash_value_typeIS3_iEENS_8equal_toIS3_EELb1EEELi1ELb1EEC2ENS_18__default_init_tagE(%"struct.std::__1::__compressed_pair_elem.13"* %0) unnamed_addr  {
  %2 = alloca %"struct.std::__1::__default_init_tag", align 1
  %3 = alloca %"struct.std::__1::__compressed_pair_elem.13"*, align 8
  store %"struct.std::__1::__compressed_pair_elem.13"* %0, %"struct.std::__1::__compressed_pair_elem.13"** %3, align 8
  %4 = load %"struct.std::__1::__compressed_pair_elem.13"*, %"struct.std::__1::__compressed_pair_elem.13"** %3, align 8
  %5 = bitcast %"struct.std::__1::__compressed_pair_elem.13"* %4 to %"class.std::__1::__unordered_map_equal"*
  call void @_ZNSt3__121__unordered_map_equalIP5ArrayNS_17__hash_value_typeIS2_iEENS_8equal_toIS2_EELb1EEC2Ev(%"class.std::__1::__unordered_map_equal"* %5) #2
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__121__unordered_map_equalIP5ArrayNS_17__hash_value_typeIS2_iEENS_8equal_toIS2_EELb1EEC2Ev(%"class.std::__1::__unordered_map_equal"* %0) unnamed_addr  {
  %2 = alloca %"class.std::__1::__unordered_map_equal"*, align 8
  store %"class.std::__1::__unordered_map_equal"* %0, %"class.std::__1::__unordered_map_equal"** %2, align 8
  %3 = load %"class.std::__1::__unordered_map_equal"*, %"class.std::__1::__unordered_map_equal"** %2, align 8
  %4 = bitcast %"class.std::__1::__unordered_map_equal"* %3 to %"struct.std::__1::equal_to"*
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__110unique_ptrIA_PNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEENS_25__bucket_list_deallocatorINS_9allocatorISB_EEEEED2Ev(%"class.std::__1::unique_ptr"* %0) unnamed_addr  {
  %2 = alloca %"class.std::__1::unique_ptr"*, align 8
  store %"class.std::__1::unique_ptr"* %0, %"class.std::__1::unique_ptr"** %2, align 8
  %3 = load %"class.std::__1::unique_ptr"*, %"class.std::__1::unique_ptr"** %2, align 8
  call void @_ZNSt3__110unique_ptrIA_PNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEENS_25__bucket_list_deallocatorINS_9allocatorISB_EEEEE5resetEDn(%"class.std::__1::unique_ptr"* %3, i8* null) #2
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__110unique_ptrIA_PNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEENS_25__bucket_list_deallocatorINS_9allocatorISB_EEEEE5resetEDn(%"class.std::__1::unique_ptr"* %0, i8* %1)  {
  %3 = alloca %"class.std::__1::unique_ptr"*, align 8
  %4 = alloca i8*, align 8
  %5 = alloca %"struct.std::__1::__hash_node_base"**, align 8
  store %"class.std::__1::unique_ptr"* %0, %"class.std::__1::unique_ptr"** %3, align 8
  store i8* %1, i8** %4, align 8
  %6 = load %"class.std::__1::unique_ptr"*, %"class.std::__1::unique_ptr"** %3, align 8
  %7 = getelementptr inbounds %"class.std::__1::unique_ptr", %"class.std::__1::unique_ptr"* %6, i32 0, i32 0
  %8 = call nonnull align 8 dereferenceable(8) %"struct.std::__1::__hash_node_base"*** @_ZNSt3__117__compressed_pairIPPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEENS_25__bucket_list_deallocatorINS_9allocatorISB_EEEEE5firstEv(%"class.std::__1::__compressed_pair"* %7) #2
  %9 = load %"struct.std::__1::__hash_node_base"**, %"struct.std::__1::__hash_node_base"*** %8, align 8
  store %"struct.std::__1::__hash_node_base"** %9, %"struct.std::__1::__hash_node_base"*** %5, align 8
  %10 = getelementptr inbounds %"class.std::__1::unique_ptr", %"class.std::__1::unique_ptr"* %6, i32 0, i32 0
  %11 = call nonnull align 8 dereferenceable(8) %"struct.std::__1::__hash_node_base"*** @_ZNSt3__117__compressed_pairIPPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEENS_25__bucket_list_deallocatorINS_9allocatorISB_EEEEE5firstEv(%"class.std::__1::__compressed_pair"* %10) #2
  store %"struct.std::__1::__hash_node_base"** null, %"struct.std::__1::__hash_node_base"*** %11, align 8
  %12 = load %"struct.std::__1::__hash_node_base"**, %"struct.std::__1::__hash_node_base"*** %5, align 8
  %13 = icmp ne %"struct.std::__1::__hash_node_base"** %12, null
  br i1 %13, label %14, label %18

14:                                               ; preds = %2
  %15 = getelementptr inbounds %"class.std::__1::unique_ptr", %"class.std::__1::unique_ptr"* %6, i32 0, i32 0
  %16 = call nonnull align 8 dereferenceable(8) %"class.std::__1::__bucket_list_deallocator"* @_ZNSt3__117__compressed_pairIPPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEENS_25__bucket_list_deallocatorINS_9allocatorISB_EEEEE6secondEv(%"class.std::__1::__compressed_pair"* %15) #2
  %17 = load %"struct.std::__1::__hash_node_base"**, %"struct.std::__1::__hash_node_base"*** %5, align 8
  call void @_ZNSt3__125__bucket_list_deallocatorINS_9allocatorIPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEEEEclEPSC_(%"class.std::__1::__bucket_list_deallocator"* %16, %"struct.std::__1::__hash_node_base"** %17) #2
  br label %18

18:                                               ; preds = %14, %2
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 8 dereferenceable(8) %"struct.std::__1::__hash_node_base"*** @_ZNSt3__117__compressed_pairIPPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEENS_25__bucket_list_deallocatorINS_9allocatorISB_EEEEE5firstEv(%"class.std::__1::__compressed_pair"* %0)  {
  %2 = alloca %"class.std::__1::__compressed_pair"*, align 8
  store %"class.std::__1::__compressed_pair"* %0, %"class.std::__1::__compressed_pair"** %2, align 8
  %3 = load %"class.std::__1::__compressed_pair"*, %"class.std::__1::__compressed_pair"** %2, align 8
  %4 = bitcast %"class.std::__1::__compressed_pair"* %3 to %"struct.std::__1::__compressed_pair_elem"*
  %5 = call nonnull align 8 dereferenceable(8) %"struct.std::__1::__hash_node_base"*** @_ZNSt3__122__compressed_pair_elemIPPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEELi0ELb0EE5__getEv(%"struct.std::__1::__compressed_pair_elem"* %4) #2
  ret %"struct.std::__1::__hash_node_base"*** %5
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 8 dereferenceable(8) %"class.std::__1::__bucket_list_deallocator"* @_ZNSt3__117__compressed_pairIPPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEENS_25__bucket_list_deallocatorINS_9allocatorISB_EEEEE6secondEv(%"class.std::__1::__compressed_pair"* %0)  {
  %2 = alloca %"class.std::__1::__compressed_pair"*, align 8
  store %"class.std::__1::__compressed_pair"* %0, %"class.std::__1::__compressed_pair"** %2, align 8
  %3 = load %"class.std::__1::__compressed_pair"*, %"class.std::__1::__compressed_pair"** %2, align 8
  %4 = bitcast %"class.std::__1::__compressed_pair"* %3 to i8*
  %5 = getelementptr inbounds i8, i8* %4, i64 8
  %6 = bitcast i8* %5 to %"struct.std::__1::__compressed_pair_elem.0"*
  %7 = call nonnull align 8 dereferenceable(8) %"class.std::__1::__bucket_list_deallocator"* @_ZNSt3__122__compressed_pair_elemINS_25__bucket_list_deallocatorINS_9allocatorIPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEEEEELi1ELb0EE5__getEv(%"struct.std::__1::__compressed_pair_elem.0"* %6) #2
  ret %"class.std::__1::__bucket_list_deallocator"* %7
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__125__bucket_list_deallocatorINS_9allocatorIPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEEEEclEPSC_(%"class.std::__1::__bucket_list_deallocator"* %0, %"struct.std::__1::__hash_node_base"** %1)  {
  %3 = alloca %"class.std::__1::__bucket_list_deallocator"*, align 8
  %4 = alloca %"struct.std::__1::__hash_node_base"**, align 8
  store %"class.std::__1::__bucket_list_deallocator"* %0, %"class.std::__1::__bucket_list_deallocator"** %3, align 8
  store %"struct.std::__1::__hash_node_base"** %1, %"struct.std::__1::__hash_node_base"*** %4, align 8
  %5 = load %"class.std::__1::__bucket_list_deallocator"*, %"class.std::__1::__bucket_list_deallocator"** %3, align 8
  %6 = call nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__125__bucket_list_deallocatorINS_9allocatorIPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEEEE7__allocEv(%"class.std::__1::__bucket_list_deallocator"* %5) #2
  %7 = load %"struct.std::__1::__hash_node_base"**, %"struct.std::__1::__hash_node_base"*** %4, align 8
  %8 = call nonnull align 8 dereferenceable(8) i64* @_ZNSt3__125__bucket_list_deallocatorINS_9allocatorIPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEEEE4sizeEv(%"class.std::__1::__bucket_list_deallocator"* %5) #2
  %9 = load i64, i64* %8, align 8
  call void @_ZNSt3__116allocator_traitsINS_9allocatorIPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEEEE10deallocateERSD_PSC_m(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %6, %"struct.std::__1::__hash_node_base"** %7, i64 %9) #2
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 8 dereferenceable(8) %"struct.std::__1::__hash_node_base"*** @_ZNSt3__122__compressed_pair_elemIPPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEELi0ELb0EE5__getEv(%"struct.std::__1::__compressed_pair_elem"* %0)  {
  %2 = alloca %"struct.std::__1::__compressed_pair_elem"*, align 8
  store %"struct.std::__1::__compressed_pair_elem"* %0, %"struct.std::__1::__compressed_pair_elem"** %2, align 8
  %3 = load %"struct.std::__1::__compressed_pair_elem"*, %"struct.std::__1::__compressed_pair_elem"** %2, align 8
  %4 = getelementptr inbounds %"struct.std::__1::__compressed_pair_elem", %"struct.std::__1::__compressed_pair_elem"* %3, i32 0, i32 0
  ret %"struct.std::__1::__hash_node_base"*** %4
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 8 dereferenceable(8) %"class.std::__1::__bucket_list_deallocator"* @_ZNSt3__122__compressed_pair_elemINS_25__bucket_list_deallocatorINS_9allocatorIPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEEEEELi1ELb0EE5__getEv(%"struct.std::__1::__compressed_pair_elem.0"* %0)  {
  %2 = alloca %"struct.std::__1::__compressed_pair_elem.0"*, align 8
  store %"struct.std::__1::__compressed_pair_elem.0"* %0, %"struct.std::__1::__compressed_pair_elem.0"** %2, align 8
  %3 = load %"struct.std::__1::__compressed_pair_elem.0"*, %"struct.std::__1::__compressed_pair_elem.0"** %2, align 8
  %4 = getelementptr inbounds %"struct.std::__1::__compressed_pair_elem.0", %"struct.std::__1::__compressed_pair_elem.0"* %3, i32 0, i32 0
  ret %"class.std::__1::__bucket_list_deallocator"* %4
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__116allocator_traitsINS_9allocatorIPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEEEE10deallocateERSD_PSC_m(%"class.std::__1::allocator"* nonnull align 1 dereferenceable(1) %0, %"struct.std::__1::__hash_node_base"** %1, i64 %2)  {
  %4 = alloca %"class.std::__1::allocator"*, align 8
  %5 = alloca %"struct.std::__1::__hash_node_base"**, align 8
  %6 = alloca i64, align 8
  store %"class.std::__1::allocator"* %0, %"class.std::__1::allocator"** %4, align 8
  store %"struct.std::__1::__hash_node_base"** %1, %"struct.std::__1::__hash_node_base"*** %5, align 8
  store i64 %2, i64* %6, align 8
  %7 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %4, align 8
  %8 = load %"struct.std::__1::__hash_node_base"**, %"struct.std::__1::__hash_node_base"*** %5, align 8
  %9 = load i64, i64* %6, align 8
  call void @_ZNSt3__19allocatorIPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEE10deallocateEPSB_m(%"class.std::__1::allocator"* %7, %"struct.std::__1::__hash_node_base"** %8, i64 %9) #2
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__125__bucket_list_deallocatorINS_9allocatorIPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEEEE7__allocEv(%"class.std::__1::__bucket_list_deallocator"* %0)  {
  %2 = alloca %"class.std::__1::__bucket_list_deallocator"*, align 8
  store %"class.std::__1::__bucket_list_deallocator"* %0, %"class.std::__1::__bucket_list_deallocator"** %2, align 8
  %3 = load %"class.std::__1::__bucket_list_deallocator"*, %"class.std::__1::__bucket_list_deallocator"** %2, align 8
  %4 = getelementptr inbounds %"class.std::__1::__bucket_list_deallocator", %"class.std::__1::__bucket_list_deallocator"* %3, i32 0, i32 0
  %5 = call nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__117__compressed_pairImNS_9allocatorIPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEEEE6secondEv(%"class.std::__1::__compressed_pair.1"* %4) #2
  ret %"class.std::__1::allocator"* %5
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 8 dereferenceable(8) i64* @_ZNSt3__125__bucket_list_deallocatorINS_9allocatorIPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEEEE4sizeEv(%"class.std::__1::__bucket_list_deallocator"* %0)  {
  %2 = alloca %"class.std::__1::__bucket_list_deallocator"*, align 8
  store %"class.std::__1::__bucket_list_deallocator"* %0, %"class.std::__1::__bucket_list_deallocator"** %2, align 8
  %3 = load %"class.std::__1::__bucket_list_deallocator"*, %"class.std::__1::__bucket_list_deallocator"** %2, align 8
  %4 = getelementptr inbounds %"class.std::__1::__bucket_list_deallocator", %"class.std::__1::__bucket_list_deallocator"* %3, i32 0, i32 0
  %5 = call nonnull align 8 dereferenceable(8) i64* @_ZNSt3__117__compressed_pairImNS_9allocatorIPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEEEE5firstEv(%"class.std::__1::__compressed_pair.1"* %4) #2
  ret i64* %5
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__19allocatorIPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEE10deallocateEPSB_m(%"class.std::__1::allocator"* %0, %"struct.std::__1::__hash_node_base"** %1, i64 %2)  personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*)  {
  %4 = alloca %"class.std::__1::allocator"*, align 8
  %5 = alloca %"struct.std::__1::__hash_node_base"**, align 8
  %6 = alloca i64, align 8
  store %"class.std::__1::allocator"* %0, %"class.std::__1::allocator"** %4, align 8
  store %"struct.std::__1::__hash_node_base"** %1, %"struct.std::__1::__hash_node_base"*** %5, align 8
  store i64 %2, i64* %6, align 8
  %7 = load %"class.std::__1::allocator"*, %"class.std::__1::allocator"** %4, align 8
  %8 = load %"struct.std::__1::__hash_node_base"**, %"struct.std::__1::__hash_node_base"*** %5, align 8
  %9 = bitcast %"struct.std::__1::__hash_node_base"** %8 to i8*
  %10 = load i64, i64* %6, align 8
  %11 = mul i64 %10, 8
  invoke void @_ZNSt3__119__libcpp_deallocateEPvmm(i8* %9, i64 %11, i64 8)
          to label %12 unwind label %13

12:                                               ; preds = %3
  ret void

13:                                               ; preds = %3
  %14 = landingpad { i8*, i32 }
          catch i8* null
  %15 = extractvalue { i8*, i32 } %14, 0
  call void @__clang_call_terminate(i8* %15) #9
  unreachable
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__119__libcpp_deallocateEPvmm(i8* %0, i64 %1, i64 %2)  {
  %4 = alloca i8*, align 8
  %5 = alloca i64, align 8
  %6 = alloca i64, align 8
  store i8* %0, i8** %4, align 8
  store i64 %1, i64* %5, align 8
  store i64 %2, i64* %6, align 8
  %7 = load i8*, i8** %4, align 8
  %8 = load i64, i64* %5, align 8
  %9 = load i64, i64* %6, align 8
  call void @_ZNSt3__117_DeallocateCaller33__do_deallocate_handle_size_alignEPvmm(i8* %7, i64 %8, i64 %9)
  ret void
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__117_DeallocateCaller33__do_deallocate_handle_size_alignEPvmm(i8* %0, i64 %1, i64 %2)  {
  %4 = alloca i8*, align 8
  %5 = alloca i64, align 8
  %6 = alloca i64, align 8
  store i8* %0, i8** %4, align 8
  store i64 %1, i64* %5, align 8
  store i64 %2, i64* %6, align 8
  %7 = load i8*, i8** %4, align 8
  %8 = load i64, i64* %5, align 8
  call void @_ZNSt3__117_DeallocateCaller27__do_deallocate_handle_sizeEPvm(i8* %7, i64 %8)
  ret void
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZNSt3__117_DeallocateCaller27__do_deallocate_handle_sizeEPvm(i8* %0, i64 %1)  {
  %3 = alloca i8*, align 8
  %4 = alloca i64, align 8
  store i8* %0, i8** %3, align 8
  store i64 %1, i64* %4, align 8
  %5 = load i8*, i8** %3, align 8
  call void @_ZNSt3__117_DeallocateCaller9__do_callEPv(i8* %5)
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr void @_ZNSt3__117_DeallocateCaller9__do_callEPv(i8* %0)  {
  %2 = alloca i8*, align 8
  store i8* %0, i8** %2, align 8
  %3 = load i8*, i8** %2, align 8
  call void @_ZdlPv(i8* %3) #8
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__117__compressed_pairImNS_9allocatorIPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEEEE6secondEv(%"class.std::__1::__compressed_pair.1"* %0)  {
  %2 = alloca %"class.std::__1::__compressed_pair.1"*, align 8
  store %"class.std::__1::__compressed_pair.1"* %0, %"class.std::__1::__compressed_pair.1"** %2, align 8
  %3 = load %"class.std::__1::__compressed_pair.1"*, %"class.std::__1::__compressed_pair.1"** %2, align 8
  %4 = bitcast %"class.std::__1::__compressed_pair.1"* %3 to %"struct.std::__1::__compressed_pair_elem.3"*
  %5 = call nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__122__compressed_pair_elemINS_9allocatorIPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEEELi1ELb1EE5__getEv(%"struct.std::__1::__compressed_pair_elem.3"* %4) #2
  ret %"class.std::__1::allocator"* %5
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 1 dereferenceable(1) %"class.std::__1::allocator"* @_ZNSt3__122__compressed_pair_elemINS_9allocatorIPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEEELi1ELb1EE5__getEv(%"struct.std::__1::__compressed_pair_elem.3"* %0)  {
  %2 = alloca %"struct.std::__1::__compressed_pair_elem.3"*, align 8
  store %"struct.std::__1::__compressed_pair_elem.3"* %0, %"struct.std::__1::__compressed_pair_elem.3"** %2, align 8
  %3 = load %"struct.std::__1::__compressed_pair_elem.3"*, %"struct.std::__1::__compressed_pair_elem.3"** %2, align 8
  %4 = bitcast %"struct.std::__1::__compressed_pair_elem.3"* %3 to %"class.std::__1::allocator"*
  ret %"class.std::__1::allocator"* %4
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 8 dereferenceable(8) i64* @_ZNSt3__117__compressed_pairImNS_9allocatorIPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEEEE5firstEv(%"class.std::__1::__compressed_pair.1"* %0)  {
  %2 = alloca %"class.std::__1::__compressed_pair.1"*, align 8
  store %"class.std::__1::__compressed_pair.1"* %0, %"class.std::__1::__compressed_pair.1"** %2, align 8
  %3 = load %"class.std::__1::__compressed_pair.1"*, %"class.std::__1::__compressed_pair.1"** %2, align 8
  %4 = bitcast %"class.std::__1::__compressed_pair.1"* %3 to %"struct.std::__1::__compressed_pair_elem.2"*
  %5 = call nonnull align 8 dereferenceable(8) i64* @_ZNSt3__122__compressed_pair_elemImLi0ELb0EE5__getEv(%"struct.std::__1::__compressed_pair_elem.2"* %4) #2
  ret i64* %5
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 8 dereferenceable(8) i64* @_ZNSt3__122__compressed_pair_elemImLi0ELb0EE5__getEv(%"struct.std::__1::__compressed_pair_elem.2"* %0)  {
  %2 = alloca %"struct.std::__1::__compressed_pair_elem.2"*, align 8
  store %"struct.std::__1::__compressed_pair_elem.2"* %0, %"struct.std::__1::__compressed_pair_elem.2"** %2, align 8
  %3 = load %"struct.std::__1::__compressed_pair_elem.2"*, %"struct.std::__1::__compressed_pair_elem.2"** %2, align 8
  %4 = getelementptr inbounds %"struct.std::__1::__compressed_pair_elem.2", %"struct.std::__1::__compressed_pair_elem.2"* %3, i32 0, i32 0
  ret i64* %4
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__113unordered_mapIP5ArrayiNS_4hashIS2_EENS_8equal_toIS2_EENS_9allocatorINS_4pairIKS2_iEEEEED2Ev(%"class.std::__1::unordered_map"* %0) unnamed_addr  {
  %2 = alloca %"class.std::__1::unordered_map"*, align 8
  store %"class.std::__1::unordered_map"* %0, %"class.std::__1::unordered_map"** %2, align 8
  %3 = load %"class.std::__1::unordered_map"*, %"class.std::__1::unordered_map"** %2, align 8
  %4 = getelementptr inbounds %"class.std::__1::unordered_map", %"class.std::__1::unordered_map"* %3, i32 0, i32 0
  call void @_ZNSt3__112__hash_tableINS_17__hash_value_typeIP5ArrayiEENS_22__unordered_map_hasherIS3_S4_NS_4hashIS3_EELb1EEENS_21__unordered_map_equalIS3_S4_NS_8equal_toIS3_EELb1EEENS_9allocatorIS4_EEED1Ev(%"class.std::__1::__hash_table"* %4) #2
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr void @_ZNSt3__112__hash_tableINS_17__hash_value_typeIP5ArrayiEENS_22__unordered_map_hasherIS3_S4_NS_4hashIS3_EELb1EEENS_21__unordered_map_equalIS3_S4_NS_8equal_toIS3_EELb1EEENS_9allocatorIS4_EEED1Ev(%"class.std::__1::__hash_table"* %0) unnamed_addr  {
  %2 = alloca %"class.std::__1::__hash_table"*, align 8
  store %"class.std::__1::__hash_table"* %0, %"class.std::__1::__hash_table"** %2, align 8
  %3 = load %"class.std::__1::__hash_table"*, %"class.std::__1::__hash_table"** %2, align 8
  call void @_ZNSt3__112__hash_tableINS_17__hash_value_typeIP5ArrayiEENS_22__unordered_map_hasherIS3_S4_NS_4hashIS3_EELb1EEENS_21__unordered_map_equalIS3_S4_NS_8equal_toIS3_EELb1EEENS_9allocatorIS4_EEED2Ev(%"class.std::__1::__hash_table"* %3) #2
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr void @_ZNSt3__112__hash_tableINS_17__hash_value_typeIP5ArrayiEENS_22__unordered_map_hasherIS3_S4_NS_4hashIS3_EELb1EEENS_21__unordered_map_equalIS3_S4_NS_8equal_toIS3_EELb1EEENS_9allocatorIS4_EEED2Ev(%"class.std::__1::__hash_table"* %0) unnamed_addr  {
  %2 = alloca %"class.std::__1::__hash_table"*, align 8
  store %"class.std::__1::__hash_table"* %0, %"class.std::__1::__hash_table"** %2, align 8
  %3 = load %"class.std::__1::__hash_table"*, %"class.std::__1::__hash_table"** %2, align 8
  %4 = getelementptr inbounds %"class.std::__1::__hash_table", %"class.std::__1::__hash_table"* %3, i32 0, i32 1
  %5 = call nonnull align 8 dereferenceable(8) %"struct.std::__1::__hash_node_base"* @_ZNSt3__117__compressed_pairINS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEENS_9allocatorIS8_EEE5firstEv(%"class.std::__1::__compressed_pair.4"* %4) #2
  %6 = getelementptr inbounds %"struct.std::__1::__hash_node_base", %"struct.std::__1::__hash_node_base"* %5, i32 0, i32 0
  %7 = load %"struct.std::__1::__hash_node_base"*, %"struct.std::__1::__hash_node_base"** %6, align 8
  call void @_ZNSt3__112__hash_tableINS_17__hash_value_typeIP5ArrayiEENS_22__unordered_map_hasherIS3_S4_NS_4hashIS3_EELb1EEENS_21__unordered_map_equalIS3_S4_NS_8equal_toIS3_EELb1EEENS_9allocatorIS4_EEE17__deallocate_nodeEPNS_16__hash_node_baseIPNS_11__hash_nodeIS4_PvEEEE(%"class.std::__1::__hash_table"* %3, %"struct.std::__1::__hash_node_base"* %7) #2
  %8 = getelementptr inbounds %"class.std::__1::__hash_table", %"class.std::__1::__hash_table"* %3, i32 0, i32 0
  call void @_ZNSt3__110unique_ptrIA_PNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEENS_25__bucket_list_deallocatorINS_9allocatorISB_EEEEED1Ev(%"class.std::__1::unique_ptr"* %8) #2
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr void @_ZNSt3__112__hash_tableINS_17__hash_value_typeIP5ArrayiEENS_22__unordered_map_hasherIS3_S4_NS_4hashIS3_EELb1EEENS_21__unordered_map_equalIS3_S4_NS_8equal_toIS3_EELb1EEENS_9allocatorIS4_EEE17__deallocate_nodeEPNS_16__hash_node_baseIPNS_11__hash_nodeIS4_PvEEEE(%"class.std::__1::__hash_table"* %0, %"struct.std::__1::__hash_node_base"* %1)  personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*)  {
  %3 = alloca %"class.std::__1::__hash_table"*, align 8
  %4 = alloca %"struct.std::__1::__hash_node_base"*, align 8
  %5 = alloca %"class.std::__1::allocator.7"*, align 8
  %6 = alloca %"struct.std::__1::__hash_node_base"*, align 8
  %7 = alloca %"struct.std::__1::__hash_node"*, align 8
  store %"class.std::__1::__hash_table"* %0, %"class.std::__1::__hash_table"** %3, align 8
  store %"struct.std::__1::__hash_node_base"* %1, %"struct.std::__1::__hash_node_base"** %4, align 8
  %8 = load %"class.std::__1::__hash_table"*, %"class.std::__1::__hash_table"** %3, align 8
  %9 = call nonnull align 1 dereferenceable(1) %"class.std::__1::allocator.7"* @_ZNSt3__112__hash_tableINS_17__hash_value_typeIP5ArrayiEENS_22__unordered_map_hasherIS3_S4_NS_4hashIS3_EELb1EEENS_21__unordered_map_equalIS3_S4_NS_8equal_toIS3_EELb1EEENS_9allocatorIS4_EEE12__node_allocEv(%"class.std::__1::__hash_table"* %8) #2
  store %"class.std::__1::allocator.7"* %9, %"class.std::__1::allocator.7"** %5, align 8
  br label %10

10:                                               ; preds = %24, %2
  %11 = load %"struct.std::__1::__hash_node_base"*, %"struct.std::__1::__hash_node_base"** %4, align 8
  %12 = icmp ne %"struct.std::__1::__hash_node_base"* %11, null
  br i1 %12, label %13, label %28

13:                                               ; preds = %10
  %14 = load %"struct.std::__1::__hash_node_base"*, %"struct.std::__1::__hash_node_base"** %4, align 8
  %15 = getelementptr inbounds %"struct.std::__1::__hash_node_base", %"struct.std::__1::__hash_node_base"* %14, i32 0, i32 0
  %16 = load %"struct.std::__1::__hash_node_base"*, %"struct.std::__1::__hash_node_base"** %15, align 8
  store %"struct.std::__1::__hash_node_base"* %16, %"struct.std::__1::__hash_node_base"** %6, align 8
  %17 = load %"struct.std::__1::__hash_node_base"*, %"struct.std::__1::__hash_node_base"** %4, align 8
  %18 = call %"struct.std::__1::__hash_node"* @_ZNSt3__116__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEE8__upcastEv(%"struct.std::__1::__hash_node_base"* %17) #2
  store %"struct.std::__1::__hash_node"* %18, %"struct.std::__1::__hash_node"** %7, align 8
  %19 = load %"class.std::__1::allocator.7"*, %"class.std::__1::allocator.7"** %5, align 8
  %20 = load %"struct.std::__1::__hash_node"*, %"struct.std::__1::__hash_node"** %7, align 8
  %21 = getelementptr inbounds %"struct.std::__1::__hash_node", %"struct.std::__1::__hash_node"* %20, i32 0, i32 2
  %22 = invoke %"struct.std::__1::pair"* @_ZNSt3__122__hash_key_value_typesINS_17__hash_value_typeIP5ArrayiEEE9__get_ptrERS4_(%"struct.std::__1::__hash_value_type"* nonnull align 8 dereferenceable(16) %21)
          to label %23 unwind label %29

23:                                               ; preds = %13
  invoke void @_ZNSt3__116allocator_traitsINS_9allocatorINS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEE7destroyINS_4pairIKS5_iEEEEvRS9_PT_(%"class.std::__1::allocator.7"* nonnull align 1 dereferenceable(1) %19, %"struct.std::__1::pair"* %22)
          to label %24 unwind label %29

24:                                               ; preds = %23
  %25 = load %"class.std::__1::allocator.7"*, %"class.std::__1::allocator.7"** %5, align 8
  %26 = load %"struct.std::__1::__hash_node"*, %"struct.std::__1::__hash_node"** %7, align 8
  call void @_ZNSt3__116allocator_traitsINS_9allocatorINS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEE10deallocateERS9_PS8_m(%"class.std::__1::allocator.7"* nonnull align 1 dereferenceable(1) %25, %"struct.std::__1::__hash_node"* %26, i64 1) #2
  %27 = load %"struct.std::__1::__hash_node_base"*, %"struct.std::__1::__hash_node_base"** %6, align 8
  store %"struct.std::__1::__hash_node_base"* %27, %"struct.std::__1::__hash_node_base"** %4, align 8
  br label %10

28:                                               ; preds = %10
  ret void

29:                                               ; preds = %23, %13
  %30 = landingpad { i8*, i32 }
          catch i8* null
  %31 = extractvalue { i8*, i32 } %30, 0
  call void @__clang_call_terminate(i8* %31) #9
  unreachable
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 8 dereferenceable(8) %"struct.std::__1::__hash_node_base"* @_ZNSt3__117__compressed_pairINS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEENS_9allocatorIS8_EEE5firstEv(%"class.std::__1::__compressed_pair.4"* %0)  {
  %2 = alloca %"class.std::__1::__compressed_pair.4"*, align 8
  store %"class.std::__1::__compressed_pair.4"* %0, %"class.std::__1::__compressed_pair.4"** %2, align 8
  %3 = load %"class.std::__1::__compressed_pair.4"*, %"class.std::__1::__compressed_pair.4"** %2, align 8
  %4 = bitcast %"class.std::__1::__compressed_pair.4"* %3 to %"struct.std::__1::__compressed_pair_elem.5"*
  %5 = call nonnull align 8 dereferenceable(8) %"struct.std::__1::__hash_node_base"* @_ZNSt3__122__compressed_pair_elemINS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEELi0ELb0EE5__getEv(%"struct.std::__1::__compressed_pair_elem.5"* %4) #2
  ret %"struct.std::__1::__hash_node_base"* %5
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 1 dereferenceable(1) %"class.std::__1::allocator.7"* @_ZNSt3__112__hash_tableINS_17__hash_value_typeIP5ArrayiEENS_22__unordered_map_hasherIS3_S4_NS_4hashIS3_EELb1EEENS_21__unordered_map_equalIS3_S4_NS_8equal_toIS3_EELb1EEENS_9allocatorIS4_EEE12__node_allocEv(%"class.std::__1::__hash_table"* %0)  {
  %2 = alloca %"class.std::__1::__hash_table"*, align 8
  store %"class.std::__1::__hash_table"* %0, %"class.std::__1::__hash_table"** %2, align 8
  %3 = load %"class.std::__1::__hash_table"*, %"class.std::__1::__hash_table"** %2, align 8
  %4 = getelementptr inbounds %"class.std::__1::__hash_table", %"class.std::__1::__hash_table"* %3, i32 0, i32 1
  %5 = call nonnull align 1 dereferenceable(1) %"class.std::__1::allocator.7"* @_ZNSt3__117__compressed_pairINS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEENS_9allocatorIS8_EEE6secondEv(%"class.std::__1::__compressed_pair.4"* %4) #2
  ret %"class.std::__1::allocator.7"* %5
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden %"struct.std::__1::__hash_node"* @_ZNSt3__116__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEE8__upcastEv(%"struct.std::__1::__hash_node_base"* %0)  {
  %2 = alloca %"struct.std::__1::__hash_node_base"*, align 8
  store %"struct.std::__1::__hash_node_base"* %0, %"struct.std::__1::__hash_node_base"** %2, align 8
  %3 = load %"struct.std::__1::__hash_node_base"*, %"struct.std::__1::__hash_node_base"** %2, align 8
  %4 = call %"struct.std::__1::__hash_node_base"* @_ZNSt3__114pointer_traitsIPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEE10pointer_toERSA_(%"struct.std::__1::__hash_node_base"* nonnull align 8 dereferenceable(8) %3) #2
  %5 = bitcast %"struct.std::__1::__hash_node_base"* %4 to %"struct.std::__1::__hash_node"*
  ret %"struct.std::__1::__hash_node"* %5
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr void @_ZNSt3__116allocator_traitsINS_9allocatorINS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEE7destroyINS_4pairIKS5_iEEEEvRS9_PT_(%"class.std::__1::allocator.7"* nonnull align 1 dereferenceable(1) %0, %"struct.std::__1::pair"* %1)  {
  %3 = alloca %"class.std::__1::allocator.7"*, align 8
  %4 = alloca %"struct.std::__1::pair"*, align 8
  %5 = alloca %"struct.std::__1::integral_constant", align 1
  %6 = alloca %"struct.std::__1::__has_destroy", align 1
  store %"class.std::__1::allocator.7"* %0, %"class.std::__1::allocator.7"** %3, align 8
  store %"struct.std::__1::pair"* %1, %"struct.std::__1::pair"** %4, align 8
  %7 = bitcast %"struct.std::__1::__has_destroy"* %6 to %"struct.std::__1::integral_constant"*
  %8 = load %"class.std::__1::allocator.7"*, %"class.std::__1::allocator.7"** %3, align 8
  %9 = load %"struct.std::__1::pair"*, %"struct.std::__1::pair"** %4, align 8
  call void @_ZNSt3__116allocator_traitsINS_9allocatorINS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEE9__destroyINS_4pairIKS5_iEEEEvNS_17integral_constantIbLb0EEERS9_PT_(%"class.std::__1::allocator.7"* nonnull align 1 dereferenceable(1) %8, %"struct.std::__1::pair"* %9)
  ret void
}

; Function Attrs: noinline optnone ssp uwtable
define linkonce_odr hidden %"struct.std::__1::pair"* @_ZNSt3__122__hash_key_value_typesINS_17__hash_value_typeIP5ArrayiEEE9__get_ptrERS4_(%"struct.std::__1::__hash_value_type"* nonnull align 8 dereferenceable(16) %0)  {
  %2 = alloca %"struct.std::__1::__hash_value_type"*, align 8
  store %"struct.std::__1::__hash_value_type"* %0, %"struct.std::__1::__hash_value_type"** %2, align 8
  %3 = load %"struct.std::__1::__hash_value_type"*, %"struct.std::__1::__hash_value_type"** %2, align 8
  %4 = call nonnull align 8 dereferenceable(12) %"struct.std::__1::pair"* @_ZNSt3__117__hash_value_typeIP5ArrayiE11__get_valueEv(%"struct.std::__1::__hash_value_type"* %3)
  %5 = call %"struct.std::__1::pair"* @_ZNSt3__19addressofINS_4pairIKP5ArrayiEEEEPT_RS6_(%"struct.std::__1::pair"* nonnull align 8 dereferenceable(12) %4) #2
  ret %"struct.std::__1::pair"* %5
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__116allocator_traitsINS_9allocatorINS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEE10deallocateERS9_PS8_m(%"class.std::__1::allocator.7"* nonnull align 1 dereferenceable(1) %0, %"struct.std::__1::__hash_node"* %1, i64 %2)  {
  %4 = alloca %"class.std::__1::allocator.7"*, align 8
  %5 = alloca %"struct.std::__1::__hash_node"*, align 8
  %6 = alloca i64, align 8
  store %"class.std::__1::allocator.7"* %0, %"class.std::__1::allocator.7"** %4, align 8
  store %"struct.std::__1::__hash_node"* %1, %"struct.std::__1::__hash_node"** %5, align 8
  store i64 %2, i64* %6, align 8
  %7 = load %"class.std::__1::allocator.7"*, %"class.std::__1::allocator.7"** %4, align 8
  %8 = load %"struct.std::__1::__hash_node"*, %"struct.std::__1::__hash_node"** %5, align 8
  %9 = load i64, i64* %6, align 8
  call void @_ZNSt3__19allocatorINS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEE10deallocateEPS7_m(%"class.std::__1::allocator.7"* %7, %"struct.std::__1::__hash_node"* %8, i64 %9) #2
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 1 dereferenceable(1) %"class.std::__1::allocator.7"* @_ZNSt3__117__compressed_pairINS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEENS_9allocatorIS8_EEE6secondEv(%"class.std::__1::__compressed_pair.4"* %0)  {
  %2 = alloca %"class.std::__1::__compressed_pair.4"*, align 8
  store %"class.std::__1::__compressed_pair.4"* %0, %"class.std::__1::__compressed_pair.4"** %2, align 8
  %3 = load %"class.std::__1::__compressed_pair.4"*, %"class.std::__1::__compressed_pair.4"** %2, align 8
  %4 = bitcast %"class.std::__1::__compressed_pair.4"* %3 to %"struct.std::__1::__compressed_pair_elem.6"*
  %5 = call nonnull align 1 dereferenceable(1) %"class.std::__1::allocator.7"* @_ZNSt3__122__compressed_pair_elemINS_9allocatorINS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEELi1ELb1EE5__getEv(%"struct.std::__1::__compressed_pair_elem.6"* %4) #2
  ret %"class.std::__1::allocator.7"* %5
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 1 dereferenceable(1) %"class.std::__1::allocator.7"* @_ZNSt3__122__compressed_pair_elemINS_9allocatorINS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEELi1ELb1EE5__getEv(%"struct.std::__1::__compressed_pair_elem.6"* %0)  {
  %2 = alloca %"struct.std::__1::__compressed_pair_elem.6"*, align 8
  store %"struct.std::__1::__compressed_pair_elem.6"* %0, %"struct.std::__1::__compressed_pair_elem.6"** %2, align 8
  %3 = load %"struct.std::__1::__compressed_pair_elem.6"*, %"struct.std::__1::__compressed_pair_elem.6"** %2, align 8
  %4 = bitcast %"struct.std::__1::__compressed_pair_elem.6"* %3 to %"class.std::__1::allocator.7"*
  ret %"class.std::__1::allocator.7"* %4
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden %"struct.std::__1::__hash_node_base"* @_ZNSt3__114pointer_traitsIPNS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEE10pointer_toERSA_(%"struct.std::__1::__hash_node_base"* nonnull align 8 dereferenceable(8) %0)  {
  %2 = alloca %"struct.std::__1::__hash_node_base"*, align 8
  store %"struct.std::__1::__hash_node_base"* %0, %"struct.std::__1::__hash_node_base"** %2, align 8
  %3 = load %"struct.std::__1::__hash_node_base"*, %"struct.std::__1::__hash_node_base"** %2, align 8
  %4 = call %"struct.std::__1::__hash_node_base"* @_ZNSt3__19addressofINS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEEEPT_RSB_(%"struct.std::__1::__hash_node_base"* nonnull align 8 dereferenceable(8) %3) #2
  ret %"struct.std::__1::__hash_node_base"* %4
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden %"struct.std::__1::__hash_node_base"* @_ZNSt3__19addressofINS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEEEPT_RSB_(%"struct.std::__1::__hash_node_base"* nonnull align 8 dereferenceable(8) %0)  {
  %2 = alloca %"struct.std::__1::__hash_node_base"*, align 8
  store %"struct.std::__1::__hash_node_base"* %0, %"struct.std::__1::__hash_node_base"** %2, align 8
  %3 = load %"struct.std::__1::__hash_node_base"*, %"struct.std::__1::__hash_node_base"** %2, align 8
  ret %"struct.std::__1::__hash_node_base"* %3
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr void @_ZNSt3__116allocator_traitsINS_9allocatorINS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEE9__destroyINS_4pairIKS5_iEEEEvNS_17integral_constantIbLb0EEERS9_PT_(%"class.std::__1::allocator.7"* nonnull align 1 dereferenceable(1) %0, %"struct.std::__1::pair"* %1)  {
  %3 = alloca %"struct.std::__1::integral_constant", align 1
  %4 = alloca %"class.std::__1::allocator.7"*, align 8
  %5 = alloca %"struct.std::__1::pair"*, align 8
  store %"class.std::__1::allocator.7"* %0, %"class.std::__1::allocator.7"** %4, align 8
  store %"struct.std::__1::pair"* %1, %"struct.std::__1::pair"** %5, align 8
  %6 = load %"struct.std::__1::pair"*, %"struct.std::__1::pair"** %5, align 8
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden %"struct.std::__1::pair"* @_ZNSt3__19addressofINS_4pairIKP5ArrayiEEEEPT_RS6_(%"struct.std::__1::pair"* nonnull align 8 dereferenceable(12) %0)  {
  %2 = alloca %"struct.std::__1::pair"*, align 8
  store %"struct.std::__1::pair"* %0, %"struct.std::__1::pair"** %2, align 8
  %3 = load %"struct.std::__1::pair"*, %"struct.std::__1::pair"** %2, align 8
  ret %"struct.std::__1::pair"* %3
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 8 dereferenceable(12) %"struct.std::__1::pair"* @_ZNSt3__117__hash_value_typeIP5ArrayiE11__get_valueEv(%"struct.std::__1::__hash_value_type"* %0)  {
  %2 = alloca %"struct.std::__1::__hash_value_type"*, align 8
  store %"struct.std::__1::__hash_value_type"* %0, %"struct.std::__1::__hash_value_type"** %2, align 8
  %3 = load %"struct.std::__1::__hash_value_type"*, %"struct.std::__1::__hash_value_type"** %2, align 8
  %4 = getelementptr inbounds %"struct.std::__1::__hash_value_type", %"struct.std::__1::__hash_value_type"* %3, i32 0, i32 0
  ret %"struct.std::__1::pair"* %4
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden void @_ZNSt3__19allocatorINS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEE10deallocateEPS7_m(%"class.std::__1::allocator.7"* %0, %"struct.std::__1::__hash_node"* %1, i64 %2)  personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*)  {
  %4 = alloca %"class.std::__1::allocator.7"*, align 8
  %5 = alloca %"struct.std::__1::__hash_node"*, align 8
  %6 = alloca i64, align 8
  store %"class.std::__1::allocator.7"* %0, %"class.std::__1::allocator.7"** %4, align 8
  store %"struct.std::__1::__hash_node"* %1, %"struct.std::__1::__hash_node"** %5, align 8
  store i64 %2, i64* %6, align 8
  %7 = load %"class.std::__1::allocator.7"*, %"class.std::__1::allocator.7"** %4, align 8
  %8 = load %"struct.std::__1::__hash_node"*, %"struct.std::__1::__hash_node"** %5, align 8
  %9 = bitcast %"struct.std::__1::__hash_node"* %8 to i8*
  %10 = load i64, i64* %6, align 8
  %11 = mul i64 %10, 32
  invoke void @_ZNSt3__119__libcpp_deallocateEPvmm(i8* %9, i64 %11, i64 8)
          to label %12 unwind label %13

12:                                               ; preds = %3
  ret void

13:                                               ; preds = %3
  %14 = landingpad { i8*, i32 }
          catch i8* null
  %15 = extractvalue { i8*, i32 } %14, 0
  call void @__clang_call_terminate(i8* %15) #9
  unreachable
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define linkonce_odr hidden nonnull align 8 dereferenceable(8) %"struct.std::__1::__hash_node_base"* @_ZNSt3__122__compressed_pair_elemINS_16__hash_node_baseIPNS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEELi0ELb0EE5__getEv(%"struct.std::__1::__compressed_pair_elem.5"* %0)  {
  %2 = alloca %"struct.std::__1::__compressed_pair_elem.5"*, align 8
  store %"struct.std::__1::__compressed_pair_elem.5"* %0, %"struct.std::__1::__compressed_pair_elem.5"** %2, align 8
  %3 = load %"struct.std::__1::__compressed_pair_elem.5"*, %"struct.std::__1::__compressed_pair_elem.5"** %2, align 8
  %4 = getelementptr inbounds %"struct.std::__1::__compressed_pair_elem.5", %"struct.std::__1::__compressed_pair_elem.5"* %3, i32 0, i32 0
  ret %"struct.std::__1::__hash_node_base"* %4
}

; Function Attrs: noinline ssp uwtable
define internal void @_GLOBAL__sub_I_runtime.cpp()  {
  call void @__cxx_global_var_init()
  call void @__cxx_global_var_init.1()
  ret void
}


!llvm.module.flags = !{!0, !1}
!llvm.ident = !{!2}

!0 = !{i32 1, !"wchar_size", i32 4}
!1 = !{i32 7, !"PIC Level", i32 2}
!2 = !{!"Homebrew clang version 11.1.0"}
