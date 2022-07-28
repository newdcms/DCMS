﻿using DCMS.Core;
using DCMS.Core.Caching;
using DCMS.Core.Domain.Configuration;
using DCMS.Core.Domain.Products;
using DCMS.Core.Domain.Sales;
using DCMS.Core.Domain.Terminals;
using DCMS.Core.Domain.Users;
using DCMS.Core.Domain.WareHouses;
using DCMS.Services.Common;
using DCMS.Services.Configuration;
using DCMS.Services.ExportImport;
using DCMS.Services.Finances;
using DCMS.Services.Logging;
using DCMS.Services.Messages;
using DCMS.Services.Products;
using DCMS.Services.Purchases;
using DCMS.Services.Sales;
using DCMS.Services.Settings;
using DCMS.Services.Terminals;
using DCMS.Services.Users;
using DCMS.Services.WareHouses;
using DCMS.ViewModel.Models.Sales;
using DCMS.Web.Framework.Mvc.Filters;
using DCMS.Web.Infrastructure.Mapper.Extensions;
using DCMS.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace DCMS.Web.Controllers
{
    /// <summary>
    /// 退货单
    /// </summary>
    public class ReturnBillController : BasePublicController
    {
        private readonly IPrintTemplateService _printTemplateService;
        private readonly IUserActivityService _userActivityService;
        private readonly IReturnBillService _returnBillService;
        private readonly IReturnReservationBillService _returnReservationBillService;
        private readonly IDistrictService _districtService;
        private readonly IWareHouseService _wareHouseService;
        private readonly IProductService _productService;
        private readonly IStockService _stockService;
        private readonly ISettingService _settingService;
        private readonly ISpecificationAttributeService _specificationAttributeService;
        private readonly IProductTierPricePlanService _productTierPricePlanService;
        private readonly IBranchService _branchService;
        private readonly IUserService _userService;
        private readonly ITerminalService _terminalService;
        private readonly IAccountingService _accountingService;
        private readonly IMediaService _mediaService;
        private readonly IRedLocker _locker;
        private readonly IExportManager _exportManager;
        private readonly IPurchaseBillService _purchaseBillService;
        private readonly ICashReceiptBillService _cashReceiptBillService;

        public ReturnBillController(
            IWorkContext workContext,
            ILogger loggerService,
            IPrintTemplateService printTemplateService,
            IUserActivityService userActivityService,
            IReturnBillService returnBillService,
            IReturnReservationBillService returnReservationBillService,
            IDistrictService districtService,
            IWareHouseService wareHouseService,
            IProductService productService,
            IStockService stockService,
            ISettingService settingService,
            ISpecificationAttributeService specificationAttributeService,
            IProductTierPricePlanService productTierPricePlanService,
            IBranchService branchService,
            IUserService userService,
            ITerminalService terminalService,
            IAccountingService accountingService,
            IMediaService mediaService,
            IStoreContext storeContext,
            INotificationService notificationService,
            IRedLocker locker,
            IExportManager exportManager,
            IPurchaseBillService purchaseBillService,
            ICashReceiptBillService cashReceiptBillService
            ) : base(workContext, loggerService, storeContext, notificationService)
        {
            _printTemplateService = printTemplateService;
            _userActivityService = userActivityService;
            _returnBillService = returnBillService;
            _returnReservationBillService = returnReservationBillService;
            _districtService = districtService;
            _wareHouseService = wareHouseService;
            _productService = productService;
            _stockService = stockService;
            _specificationAttributeService = specificationAttributeService;
            _productTierPricePlanService = productTierPricePlanService;
            _settingService = settingService;
            _branchService = branchService;
            _userService = userService;
            _terminalService = terminalService;
            _accountingService = accountingService;
            _mediaService = mediaService;
            _locker = locker;
            _exportManager = exportManager;
            _purchaseBillService = purchaseBillService;
            _cashReceiptBillService = cashReceiptBillService;
        }

        public IActionResult Index()
        {
            return RedirectToAction("List");
        }

        [AuthCode((int)AccessGranularityEnum.ReturnBillsView)]
        public IActionResult List(int? terminalId, string terminalName, int? businessUserId, int? deliveryUserId, int? wareHouseId, int? districtId, string remark = "", string billNumber = "", DateTime? startTime = null, DateTime? endTime = null, bool? auditedStatus = null, bool? sortByAuditedTime = null, bool? showReverse = null, bool? showReturn = null, int? paymentMethodType = null, int? billSourceType = null, int? productId = 0, string productName = "", int pagenumber = 0)
        {


            var model = new ReturnBillListModel();

            if (pagenumber > 0)
            {
                pagenumber -= 1;
            }

            #region 绑定数据源

            model = PrepareReturnBillListModel(model);

            model.TerminalId = terminalId ?? null;
            model.TerminalName = terminalName;
            model.BusinessUserId = businessUserId ?? null;
            model.DeliveryUserId = deliveryUserId ?? null;
            model.BillNumber = billNumber;
            model.WareHouseId = wareHouseId ?? null;
            model.Remark = remark;
            model.StartTime = startTime ?? DateTime.Parse(DateTime.Now.ToString("yyyy-MM-01"));
            model.EndTime = endTime ?? DateTime.Now.AddDays(1);
            model.DistrictId = districtId ?? null;
            model.AuditedStatus = auditedStatus;
            model.SortByAuditedTime = sortByAuditedTime;
            model.ShowReverse = showReverse;
            model.ShowReturn = showReturn;
            model.PaymentMethodType = (paymentMethodType ?? 0);
            model.BillSourceType = (billSourceType ?? null);
            model.ProductId = productId ?? 0;
            model.ProductName = productName;
            #endregion

            var returns = _returnBillService.GetReturnBillList(curStore?.Id ?? 0,
                 curUser.Id,
                terminalId,
                terminalName,
                businessUserId,
                deliveryUserId,
                billNumber,
                wareHouseId,
                remark,
                model.StartTime,
                model.EndTime,
                districtId,
                auditedStatus,
                sortByAuditedTime,
                showReverse,
                paymentMethodType,
                billSourceType,
                null,
                false,//未删除单据
                null,
                productId,
                pagenumber,
                pageSize: 30);

            //默认收款账户动态列
            var defaultAcc = _accountingService.GetDefaultAccounting(curStore?.Id ?? 0, BillTypeEnum.ReturnBill);
            model.DynamicColumns = defaultAcc?.Item4?.OrderBy(s => s.Key).Select(s => s.Value).ToList();

            model.PagingFilteringContext.LoadPagedList(returns);

            #region 查询需要关联其他表的数据
            List<int> userIds = new List<int>();
            userIds.AddRange(returns.Select(b => b.BusinessUserId).Distinct().ToArray());
            userIds.AddRange(returns.Select(b => b.DeliveryUserId).Distinct().ToArray());

            var allUsers = _userService.GetUsersDictsByIds(curStore.Id, userIds.Distinct().ToArray());

            var allTerminal = _terminalService.GetTerminalsByIds(curStore.Id, returns.Select(b => b.TerminalId).Distinct().ToArray());
            //var allWareHouses = _wareHouseService.GetWareHouseByIds(curStore.Id, returns.Select(b => b.WareHouseId).Distinct().ToArray());
            #endregion

            model.Lists = returns.Select(s =>
            {
                var m = s.ToModel<ReturnBillModel>();

                //业务员名称
                m.BusinessUserName = allUsers.Where(aw => aw.Key == m.BusinessUserId).Select(aw => aw.Value).FirstOrDefault();
                //送货员名称
                m.DeliveryUserName = allUsers.Where(aw => aw.Key == m.DeliveryUserId).Select(aw => aw.Value).FirstOrDefault();

                //客户名称
                var terminal = allTerminal.Where(at => at.Id == m.TerminalId).FirstOrDefault();
                m.TerminalName = terminal == null ? "" : terminal.Name;
                m.TerminalPointCode = terminal == null ? "" : terminal.Code;
                //仓库名称
                var warehouse = model.WareHouses.Where(s => s.Value == m.WareHouseId.ToString()).FirstOrDefault();
                m.WareHouseName = warehouse == null ? "" : warehouse.Text;

                //应收金额	
                m.ReceivableAmount = (s.ReceivableAmount == 0) ? ((s.OweCash != 0) ? s.OweCash : s.ReturnBillAccountings.Sum(sa => sa.CollectionAmount)) : s.ReceivableAmount;

                //收款账户
                m.ReturnBillAccountings = defaultAcc?.Item4?.OrderBy(sb => sb.Key).Select(sb =>
                {
                    var acc = s.ReturnBillAccountings.Where(a => a?.AccountingOption?.ParentId == sb.Key).FirstOrDefault();
                    return new ReturnBillAccountingModel()
                    {
                        AccountingOptionId = acc?.AccountingOptionId ?? 0,
                        CollectionAmount = acc?.CollectionAmount ?? 0
                    };
                }).ToList();


                //查询退货单 关联的退货订单
                ReturnReservationBill returnReservationBill = _returnReservationBillService.GetReturnReservationBillById(curStore.Id, m.ReturnReservationBillId);
                if (returnReservationBill != null)
                {
                    m.ReturnReservationBillId = returnReservationBill.Id;
                    m.ReturnReservationBillNumber = returnReservationBill.BillNumber;
                }

                return m;
            }).ToList();

            return View(model);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="terminalId"></param>
        /// <param name="businessUserId"></param>
        /// <param name="billNumber"></param>
        /// <param name="wareHouseId"></param>
        /// <param name="remark"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="districtId"></param>
        /// <param name="auditedStatus"></param>
        /// <param name="sortByAuditedTime"></param>
        /// <param name="showReverse"></param>
        /// <param name="showReturn"></param>
        /// <param name="paymentMethodType"></param>
        /// <param name="billSourceType"></param>
        /// <param name="receipted"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [AuthCode((int)AccessGranularityEnum.ReturnBillsView)]
        public async Task<JsonResult> AsyncList(int? terminalId, int? businessUserId, int? deliveryUserId, string billNumber = "", int? wareHouseId = null, string remark = "", DateTime? start = null, DateTime? end = null, int? districtId = null, bool? auditedStatus = null, bool? sortByAuditedTime = null, bool? showReverse = null, bool? showReturn = null, int? paymentMethodType = null, int? billSourceType = null, bool? receipted = null, int? productId = 0,  int pageIndex = 0, int pageSize = 20)
        {
            return await Task.Run(() =>
            {
                var gridModel = _returnBillService.GetReturnBillList(curStore?.Id ?? 0, 
                    curUser.Id,
                    terminalId, "", 
                    businessUserId,
                    deliveryUserId, 
                    billNumber,
                    wareHouseId,
                    remark, 
                    start, 
                    end, 
                    districtId,
                    auditedStatus, 
                    sortByAuditedTime, 
                    showReverse, 
                    paymentMethodType, 
                    billSourceType, 
                    null, 
                    false,
                    null, 
                    productId, 
                    pageIndex, 
                    pageSize);


                //默认收款账户动态列
                var defaultAcc = _accountingService.GetDefaultAccounting(curStore?.Id ?? 0, BillTypeEnum.ReturnBill);

                return Json(new
                {
                    Success = true,
                    total = gridModel.TotalCount,
                    rows = gridModel.Select(s =>
                    {

                        var m = s.ToModel<ReturnBillModel>();

                        //业务员名称
                        m.BusinessUserName = _userService.GetUserName(curStore.Id, m.BusinessUserId ?? 0);

                        //客户名称
                        var terminal = _terminalService.GetTerminalById(curStore.Id, m.TerminalId);
                        m.TerminalName = terminal == null ? "" : terminal.Name;
                        m.TerminalPointCode = terminal == null ? "" : terminal.Code;
                        //仓库名称
                        m.WareHouseName = _wareHouseService.GetWareHouseName(curStore.Id, m.WareHouseId);

                        //应收金额	
                        m.ReceivableAmount = (s.ReceivableAmount == 0) ? ((s.OweCash != 0) ? s.OweCash : s.ReturnBillAccountings.Sum(sa => sa.CollectionAmount)) : s.ReceivableAmount;

                        //优惠金额
                        m.PreferentialAmount = s.PreferentialAmount;

                        //收款账户
                        m.ReturnBillAccountings = defaultAcc?.Item4?.OrderBy(sb => sb.Key).Select(sb =>
                        {
                            var acc = s.ReturnBillAccountings.Where(a => a?.AccountingOption?.ParentId == sb.Key).FirstOrDefault();
                            return new ReturnBillAccountingModel()
                            {
                                AccountingOptionId = acc?.AccountingOptionId ?? 0,
                                CollectionAmount = acc?.CollectionAmount ?? 0
                            };
                        }).ToList();

                        //欠款金额	
                        m.OweCash = s.OweCash;

                        return m;

                    }).ToList()
                });

            });
        }

        /// <summary>
        /// 添加退货单
        /// </summary>
        /// <returns></returns>
        [AuthCode((int)AccessGranularityEnum.ReturnBillsSave)]
        public IActionResult Create(int? orderId = 0)
        {

            ReturnBillModel model = new ReturnBillModel();

            #region 绑定数据源
            var companySetting = _settingService.LoadSetting<CompanySetting>(curStore.Id);
            model = PrepareReturnBillModel(model);

            #endregion

            model.PreferentialAmount = 0;
            model.PreferentialEndAmount = 0;
            model.OweCash = 0;
            model.TransactionDate = DateTime.Now;


            //单号
            model.BillNumber = CommonHelper.GetBillNumber(CommonHelper.GetEnumDescription(BillTypeEnum.ReturnBill).Split(',')[1], curStore.Id);
            model.BillBarCode = _mediaService.GenerateBarCodeForBase64(model.BillNumber, 150, 50);
            //制单人
            var mu = _userService.GetUserById(curStore.Id, curUser.Id);
            model.MakeUserName = mu != null ? (mu.UserRealName + " " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")) : "";

            //获取默认付款账户
            var defaultAcc = _accountingService.GetDefaultAccounting(curStore?.Id ?? 0, BillTypeEnum.ReturnBill);
            model.ReturnBillAccountings.Add(new ReturnBillAccountingModel()
            {
                Name = defaultAcc?.Item1?.Name,
                CollectionAmount = 0,
                AccountingOptionId = defaultAcc?.Item1?.Id ?? 0
            });


            model.IsShowCreateDate = companySetting.OpenBillMakeDate == 0 ? false : true;
            //商品变价参考
            model.VariablePriceCommodity = companySetting.VariablePriceCommodity;
            //单据合计精度
            model.AccuracyRounding = companySetting.AccuracyRounding;
            //交易日期可选范围
            model.AllowSelectionDateRange = companySetting.AllowSelectionDateRange;
            //允许预收款支付成负数
            model.AllowAdvancePaymentsNegative = companySetting.AllowAdvancePaymentsNegative;
            //启用税务功能
            model.EnableTaxRate = companySetting.EnableTaxRate;
            model.TaxRate = companySetting.TaxRate;

            //转单
            if (orderId.HasValue && orderId.Value > 0)
            {
                ReturnReservationBill order = _returnReservationBillService.GetReturnReservationBillById(curStore.Id, orderId.Value, true);
                model.OrderId = orderId.Value;
                model.OrderNumber = order?.BillNumber;
                model.TerminalId = order?.TerminalId ?? 0;
                model.TerminalName = _terminalService.GetTerminalName(curStore.Id, order?.TerminalId ?? 0);
                model.BusinessUserId = order?.BusinessUserId;
                model.WareHouseId = order?.WareHouseId ?? 0;

                //收款账户
                model.ReturnBillAccountings = order.ReturnReservationBillAccountings.Select(s =>
                {
                    return new ReturnBillAccountingModel()
                    {
                        Name = s?.AccountingOption?.Name,
                        CollectionAmount = s?.CollectionAmount ?? 0,
                        AccountingOptionId = s?.AccountingOptionId ?? 0
                    };
                }).ToList();

                //追加欠款
                model.ReturnBillAccountings.Add(new ReturnBillAccountingModel()
                {
                    Name = defaultAcc?.Item1?.Name,
                    CollectionAmount = order?.OweCash ?? 0,
                    AccountingOptionId = defaultAcc?.Item1?.Id ?? 0
                });

                model.ReturnBillAccountings.OrderBy(a => a.AccountingOptionId);
            }

            return View(model);
        }

        /// <summary>
        /// 编辑退货单
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [AuthCode((int)AccessGranularityEnum.ReturnBillsView)]
        public IActionResult Edit(int? id)
        {

            //没有值跳转到列表
            if (id == null)
            {
                return RedirectToAction("List");
            }
            var model = new ReturnBillModel();
            var companySetting = _settingService.LoadSetting<CompanySetting>(curStore.Id);

            var bill = _returnBillService.GetReturnBillById(curStore.Id, id.Value, true);

            //没有值跳转到列表
            if (bill == null || bill.StoreId != curStore.Id)
            {
                return RedirectToAction("List");
            }


            model = bill.ToModel<ReturnBillModel>();
            model.BillBarCode = _mediaService.GenerateBarCodeForBase64(bill.BillNumber, 150, 50);

            //获取默认付款账户
            var defaultAcc = _accountingService.GetDefaultAccounting(curStore?.Id ?? 0, BillTypeEnum.ReturnBill);
            model.CollectionAccount = defaultAcc?.Item1?.Id ?? 0;
            model.CollectionAmount = bill.ReturnBillAccountings.Where(sa => sa.AccountingOptionId == defaultAcc?.Item1?.Id).Sum(sa => sa.CollectionAmount);
            model.ReturnBillAccountings = bill.ReturnBillAccountings.Select(s =>
            {
                var m = s.ToAccountModel<ReturnBillAccountingModel>();
                //m.Name = _accountingService.GetAccountingOptionById(s.AccountingOptionId).Name;
                m.Name = _accountingService.GetAccountingOptionName(curStore.Id, s.AccountingOptionId);
                return m;
            }).ToList();

            //取单据项目
            model.Items = bill.Items.Select(s => s.ToModel<ReturnItemModel>()).ToList();

            //获取客户名称
            var terminal = _terminalService.GetTerminalById(curStore.Id, model.TerminalId);
            model.TerminalName = terminal == null ? "" : terminal.Name;
            model.TerminalPointCode = terminal == null ? "" : terminal.Code;

            #region 绑定数据源

            model = PrepareReturnBillModel(model);

            #endregion

            //制单人
            //var mu = _userService.GetUserById(curStore.Id, bill.MakeUserId);
            //model.MakeUserName = mu != null ? (mu.UserRealName + " " + bill.CreatedOnUtc.ToString("yyyy/MM/dd HH:mm:ss")) : "";
            var mu = string.Empty;
            if (bill.MakeUserId > 0)
            {
                mu = _userService.GetUserName(curStore.Id, bill.MakeUserId);
            }
            model.MakeUserName = mu + " " + bill.CreatedOnUtc.ToString("yyyy/MM/dd HH:mm:ss");

            //审核人
            //var au = _userService.GetUserById(curStore.Id, bill.AuditedUserId ?? 0);
            //model.AuditedUserName = au != null ? (au.UserRealName + " " + (bill.AuditedDate.HasValue ? bill.AuditedDate.Value.ToString("yyyy/MM/dd HH:mm:ss") : "")) : "";
            var au = string.Empty;
            if (bill.AuditedUserId != null && bill.AuditedUserId > 0)
            {
                au = _userService.GetUserName(curStore.Id, bill.AuditedUserId ?? 0);
            }
            model.AuditedUserName = au + " " + (bill.AuditedDate.HasValue ? bill.AuditedDate.Value.ToString("yyyy/MM/dd HH:mm:ss") : "");

            //优惠后金额
            model.PreferentialEndAmount = model.ReceivableAmount - model.PreferentialAmount;
            model.IsShowCreateDate = companySetting.OpenBillMakeDate == 0 ? false : true;
            //商品变价参考
            model.VariablePriceCommodity = companySetting.VariablePriceCommodity;
            //单据合计精度
            model.AccuracyRounding = companySetting.AccuracyRounding;
            //交易日期可选范围
            model.AllowSelectionDateRange = companySetting.AllowSelectionDateRange;
            //允许预收款支付成负数
            model.AllowAdvancePaymentsNegative = companySetting.AllowAdvancePaymentsNegative;
            //启用税务功能
            model.EnableTaxRate = companySetting.EnableTaxRate;
            model.TaxRate = companySetting.TaxRate;
            return View(model);
        }

        #region 单据项目

        /// <summary>
        /// 异步获取退货单项目
        /// </summary>
        /// <param name="returnId"></param>
        /// <returns></returns>
        public JsonResult AsyncReturnItems(int returnBillId)
        {

            var gridModel = _returnBillService.GetReturnItemList(returnBillId);

            var allProducts = _productService.GetProductsByIds(curStore.Id, gridModel.Select(pr => pr.ProductId).Distinct().ToArray());
            var allOptions = _specificationAttributeService.GetSpecificationAttributeOptionByIds(curStore.Id, allProducts.GetProductBigStrokeSmallUnitIds());
            var allProductPrices = _productService.GetProductPricesByProductIds(curStore.Id, gridModel.Select(gm => gm.ProductId).Distinct().ToArray());
            var allProductTierPrices = _productService.GetProductTierPriceByProductIds(curStore.Id, gridModel.Select(pr => pr.ProductId).Distinct().ToArray());

            var details = gridModel.Select(o =>
            {
                var m = o.ToModel<ReturnItemModel>();
                var product = allProducts.Where(ap => ap.Id == m.ProductId).FirstOrDefault();
                if (product != null)
                {
                    //这里替换成高级用法
                    m = product.InitBaseModel<ReturnItemModel>(m, o.ReturnBill.WareHouseId, allOptions, allProductPrices, allProductTierPrices, _productService);

                    //商品信息
                    m.BigUnitId = product.BigUnitId;
                    m.StrokeUnitId = product.StrokeUnitId;
                    m.SmallUnitId = product.SmallUnitId;
                    m.IsManufactureDete = product.IsManufactureDete;
                    m.ProductTimes = _productService.GetProductDates(curStore.Id, product.Id, o.ReturnBill.WareHouseId);

                    //税价总计
                    m.TaxPriceAmount = m.Amount;

                    if (o.ReturnBill.TaxAmount > 0 && m.TaxRate > 0)
                    {
                        //含税价格
                        m.ContainTaxPrice = m.Price;

                        //税额
                        m.TaxPrice = m.Amount - m.Amount / (1 + m.TaxRate / 100);

                        m.Price /= (1 + m.TaxRate / 100);
                        m.Amount /= (1 + m.TaxRate / 100);
                    }

                }
                return m;

            }).ToList();

            return Json(new
            {
                total = details.Count,
                hidden = details.Count(c => c.TaxRate > 0) > 0,
                rows = details
            });
        }

        /// <summary>
        /// 更新/编辑收款单项目
        /// </summary>
        /// <param name="data"></param>
        /// <param name="returnId"></param>
        /// <returns></returns>
        [HttpPost]
        [AuthCode((int)AccessGranularityEnum.ReturnBillsSave)]
        public async Task<JsonResult> CreateOrUpdate(ReturnBillUpdateModel data, int? billId,bool doAudit = true)
        {
            try
            {

                var bill = new ReturnBill();

                #region 单据验证
                if (data == null || data.Items == null)
                {
                    return Warning("请录入数据.");
                }

                if (PeriodLocked(DateTime.Now))
                {
                    return Warning("锁账期间,禁止业务操作.");
                }

                if (PeriodClosed(DateTime.Now))
                {
                    return Warning("会计期间已结账,禁止业务操作.");
                }

                if (billId.HasValue && billId.Value != 0)
                {

                    bill = _returnBillService.GetReturnBillById(curStore.Id, billId.Value, true);

                    //公共单据验证
                    var commonBillChecking = BillChecking<ReturnBill, ReturnItem>(bill, BillStates.Draft, ((int)AccessGranularityEnum.ReturnBillsSave).ToString());
                    if (commonBillChecking.Value != null)
                    {
                        return commonBillChecking;
                    }
                }
                #endregion

                //业务逻辑
                var accountings = _accountingService.GetAllAccountingOptions(curStore.Id, 0, true);
                var dataTo = data.ToEntity<ReturnBillUpdate>();
                dataTo.Operation = (int)OperationEnum.PC;
                if (data.Accounting == null)
                {
                    return Warning("没有默认的付款账号");
                }
                dataTo.Accounting = data.Accounting.Select(ac =>
                {
                    return ac.ToAccountEntity<ReturnBillAccounting>();
                }).ToList();
                dataTo.Items = data.Items.Select(it =>
                {

                    //成本价（此处计算成本价防止web、api成本价未带出,web、api的controller都要单独计算（取消service计算，防止其他service都引用 pruchasebillservice））
                    var item = it.ToEntity<ReturnItem>();
                    item.CostPrice = _purchaseBillService.GetReferenceCostPrice(curStore.Id, item.ProductId, item.UnitId);
                    item.CostAmount = item.CostPrice * item.Quantity;
                    return item;

                }).ToList();

                //RedLock
                var result = await _locker.PerformActionWithLockAsync(LockKey(data),
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(1),
                    () => _returnBillService.BillCreateOrUpdate(curStore.Id, curUser.Id, billId, bill, dataTo.Accounting, accountings, dataTo, dataTo.Items, new List<ProductStockItem>(), _userService.IsAdmin(curStore.Id, curUser.Id), doAudit));


                if (result.Success && data.OrderId > 0)
                {
                    _returnReservationBillService.ChangedBill(data.OrderId, curUser.Id);
                }

                return Json(result);

            }
            catch (Exception ex)
            {
                //活动日志
                _userActivityService.InsertActivity("CreateOrUpdate", Resources.Bill_CreateOrUpdateFailed, curUser.Id);
                _notificationService.SuccessNotification(Resources.Bill_CreateOrUpdateFailed);
                return Error(ex.Message);
            }
        }
        #endregion

        /// <summary>
        /// 审核
        /// </summary>
        /// <returns></returns>
        [AuthCode((int)AccessGranularityEnum.ReturnBillsApproved)]
        public async Task<JsonResult> Auditing(int? id)
        {
            try
            {
                var bill = new ReturnBill();

                #region 验证
                if (!id.HasValue)
                {
                    return Warning("参数错误.");
                }
                else
                {
                    bill = _returnBillService.GetReturnBillById(curStore.Id, id.Value, true);
                    if (bill.AuditedStatus)
                    {
                        return Warning("单据已审核，请刷新页面.");
                    }
                }

                //公共单据验证
                var commonBillChecking = BillChecking<ReturnBill, ReturnItem>(bill, BillStates.Audited, ((int)AccessGranularityEnum.ReturnBillsApproved).ToString());
                if (commonBillChecking.Value != null)
                {
                    return commonBillChecking;  
                }

                if (_wareHouseService.CheckProductInventory(curStore.Id, bill.WareHouseId, bill.Items?.Select(it => it.ProductId).Distinct().ToArray(), out string thisMsg))
                {
                    return Warning("仓库正在盘点中，拒绝操作");
                }
                #endregion

                //RedLock
                var result = await _locker.PerformActionWithLockAsync(RedLockKey(bill),
                      TimeSpan.FromSeconds(30),
                      TimeSpan.FromSeconds(10),
                      TimeSpan.FromSeconds(1),
                      () => _returnBillService.Auditing(curStore.Id, curUser.Id, bill));
                return Json(result);

            }
            catch (Exception ex)
            {
                //活动日志
                _userActivityService.InsertActivity("Auditing", "单据审核失败", curUser.Id);
                _notificationService.SuccessNotification("单据审核失败");
                return Error(ex.Message);
            }

        }

        /// <summary>
        /// 红冲
        /// </summary>
        /// <returns></returns>
        [AuthCode((int)AccessGranularityEnum.ReturnBillsReverse)]
        public async Task<JsonResult> Reverse(int? id)
        {
            try
            {

                var bill = new ReturnBill() { StoreId = curStore?.Id ?? 0 };

                #region 验证

                if (PeriodClosed(DateTime.Now))
                {
                    return Warning("系统当月已经结转，不允许红冲.");
                }

                if (!id.HasValue)
                {
                    return Warning("参数错误.");
                }
                else
                {
                    bill = _returnBillService.GetReturnBillById(curStore.Id, id.Value, true);
                    if (bill.ReversedStatus)
                    {
                        return Warning("单据已红冲，请刷新页面.");
                    }
                }

                //公共单据验证
                var commonBillChecking = BillChecking<ReturnBill, ReturnItem>(bill, BillStates.Reversed, ((int)AccessGranularityEnum.ReturnBillsReverse).ToString());
                if (commonBillChecking.Value != null)
                {
                    return commonBillChecking;
                }

                if (_wareHouseService.CheckProductInventory(curStore.Id, bill.WareHouseId, bill.Items?.Select(it => it.ProductId).Distinct().ToArray(), out string thisMsg))
                {
                    return Warning("仓库正在盘点中，拒绝操作.");
                }

                //验证是否收款
                var cashReceipt = _cashReceiptBillService.CheckBillCashReceipt(curStore.Id, (int)BillTypeEnum.ReturnBill, bill.BillNumber);
                if (cashReceipt.Item1)
                {
                    return Warning($"单据在收款单:{cashReceipt.Item2}中已经收款.");
                }

                #region 验证库存
                //将一个单据中 相同商品 数量 按最小单位汇总
                List<ProductStockItem> productStockItems = new List<ProductStockItem>();
                var allProducts = _productService.GetProductsByIds(curStore.Id, bill.Items.Select(pr => pr.ProductId).Distinct().ToArray());
                var allOptions = _specificationAttributeService.GetSpecificationAttributeOptionByIds(curStore.Id, allProducts.GetProductBigStrokeSmallUnitIds());

                foreach (ReturnItem item in bill.Items)
                {
                    var product = allProducts.Where(ap => ap.Id == item.ProductId).FirstOrDefault();
                    ProductStockItem productStockItem = productStockItems.Where(a => a.ProductId == item.ProductId).FirstOrDefault();
                    //商品转化量
                    var conversionQuantity = product.GetConversionQuantity(allOptions, item.UnitId);
                    //库存量增量 = 单位转化量 * 数量
                    int thisQuantity = item.Quantity * conversionQuantity;
                    if (productStockItem != null)
                    {
                        productStockItem.Quantity += thisQuantity;
                    }
                    else
                    {
                        productStockItem = new ProductStockItem
                        {
                            ProductId = item.ProductId,
                            //当期选择单位
                            UnitId = product.SmallUnitId,
                            //转化单位
                            SmallUnitId = product.SmallUnitId,
                            //转化单位
                            BigUnitId = product.BigUnitId ?? 0,
                            ProductName = allProducts.Where(s => s.Id == item.ProductId).FirstOrDefault()?.Name,
                            ProductCode = allProducts.Where(s => s.Id == item.ProductId).FirstOrDefault()?.ProductCode,
                            Quantity = thisQuantity
                        };

                        productStockItems.Add(productStockItem);
                    }
                }

                //验证当前商品库存
                if (!_stockService.CheckStockQty(_productService, _specificationAttributeService, bill.StoreId, bill.WareHouseId, productStockItems, out string thisMsg2))
                {
                    return Warning(thisMsg2);
                }
                #endregion


                #endregion

                //RedLock
                var result = await _locker.PerformActionWithLockAsync(RedLockKey(bill),
                      TimeSpan.FromSeconds(30),
                      TimeSpan.FromSeconds(10),
                      TimeSpan.FromSeconds(1),
                      () => _returnBillService.Reverse(curUser.Id, bill));
                return Json(result);

            }
            catch (Exception ex)
            {
                //活动日志
                _userActivityService.InsertActivity("Reverse", "单据红冲失败", curUser.Id);
                _notificationService.SuccessNotification("单据红冲失败");
                return Error(ex.Message);
            }
        }

        /// <summary>
        /// 作废
        /// </summary>
        /// <returns></returns>
        [AuthCode((int)AccessGranularityEnum.ReturnBillsDelete)]
        public JsonResult Delete(int? id)
        {
            try
            {
                var bill = new ReturnBill() { StoreId = curStore?.Id ?? 0 };

                #region 验证
                if (!id.HasValue)
                {
                    return Warning("参数错误.");
                }
                else
                {
                    bill = _returnBillService.GetReturnBillById(curStore.Id, id.Value, true);
                    if (bill.AuditedStatus || bill.ReversedStatus)
                    {
                        return Warning("单据已审核或红冲，不能作废.");
                    }
                    if (bill.Deleted)
                    {
                        return Warning("单据已作废，请刷新页面.");
                    }
                    if (bill != null)
                    {
                        var rs = _returnBillService.Delete(curUser.Id, bill);
                        if (!rs.Success)
                        {
                            return Error(rs.Message);
                        }
                    }
                }
                #endregion
                return Successful("作废成功");

            }
            catch (Exception ex)
            {
                //活动日志
                _userActivityService.InsertActivity("Delete", "单据作废失败", curUser.Id);
                _notificationService.SuccessNotification("单据作废失败");
                return Error(ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="terminalId"></param>
        /// <param name="businessUserId"></param>
        /// <param name="billNumber"></param>
        /// <param name="wareHouseId"></param>
        /// <param name="remark"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="districtId"></param>
        /// <param name="auditedStatus"></param>
        /// <param name="sortByAuditedTime"></param>
        /// <param name="showReverse"></param>
        /// <param name="showReturn"></param>
        /// <param name="paymentMethodType"></param>
        /// <param name="billSourceType"></param>
        /// <returns></returns>
        [HttpGet]
        [AuthCode((int)AccessGranularityEnum.ReturnBillsExport)]
        public FileResult Export(int type, string selectData, int? terminalId, string terminalName, int? businessUserId, int? deliveryUserId, string billNumber = "", int? wareHouseId = null, string remark = "", DateTime? startTime = null, DateTime? endTime = null, int? districtId = null, bool? auditedStatus = null, bool? sortByAuditedTime = null, bool? showReverse = null, bool? showReturn = null, int? paymentMethodType = null, int? billSourceType = null)
        {

            #region 查询导出数据

            IList<ReturnBill> returnBills = new List<ReturnBill>();
            if (type == 1)
            {
                if (!string.IsNullOrEmpty(selectData))
                {
                    List<string> ids = selectData.Split(',').ToList();
                    foreach (var id in ids)
                    {
                        ReturnBill returnBill = _returnBillService.GetReturnBillById(curStore.Id, int.Parse(id), true);
                        if (returnBill != null)
                        {
                            returnBills.Add(returnBill);
                        }
                    }
                }
            }
            else if (type == 2)
            {
                returnBills = _returnBillService.GetReturnBillList(curStore?.Id ?? 0,
                     curUser.Id,
                        terminalId,
                        terminalName,
                        businessUserId,
                        deliveryUserId,
                        billNumber,
                        wareHouseId,
                        remark,
                        startTime,
                        endTime,
                        districtId,
                        auditedStatus,
                        sortByAuditedTime,
                        showReverse,
                        paymentMethodType,
                        billSourceType,
                        null, false, null, 0);
            }

            #endregion

            #region 导出
            var ms = _exportManager.ExportReturnBillToXlsx(returnBills, curStore.Id);
            if (ms != null)
            {
                return File(ms, "application/vnd.ms-excel", "退货单.xlsx");
            }
            else
            {
                return File(new MemoryStream(), "application/vnd.ms-excel", "退货单.xlsx");
            }
            #endregion

        }

        [AuthCode((int)AccessGranularityEnum.ReturnBillsPrint)]
        public JsonResult PrintSetting()
        {
            var printTemplates = _printTemplateService.GetAllPrintTemplates(curStore.Id).ToList();
            var printTemplate = printTemplates.Where(a => a.BillType == (int)BillTypeEnum.ReturnBill).FirstOrDefault();
            //获取打印设置
            var pCPrintSetting = _settingService.LoadSetting<PCPrintSetting>(_storeContext.CurrentStore.Id);
            var settings = new object();
            if (pCPrintSetting != null)
            {
                settings = new
                {
                    PaperWidth = (printTemplate?.PaperWidth == 0 || printTemplate?.PaperHeight == 0) ? pCPrintSetting.PaperWidth : printTemplate.PaperWidth,
                    PaperHeight = (printTemplate?.PaperWidth == 0 || printTemplate?.PaperHeight == 0) ? pCPrintSetting.PaperHeight : printTemplate.PaperHeight,
                    BorderType = pCPrintSetting.BorderType,
                    MarginTop = pCPrintSetting.MarginTop,
                    MarginBottom = pCPrintSetting.MarginBottom,
                    MarginLeft = pCPrintSetting.MarginLeft,
                    MarginRight = pCPrintSetting.MarginRight,
                    IsPrintPageNumber = pCPrintSetting.IsPrintPageNumber,
                    PrintHeader = pCPrintSetting.PrintHeader,
                    PrintFooter = pCPrintSetting.PrintFooter,
                    FixedRowNumber = pCPrintSetting.FixedRowNumber,
                    PrintSubtotal = pCPrintSetting.PrintSubtotal,
                    PrintPort = pCPrintSetting.PrintPort
                };
                return Successful("", settings);
            }
            return Successful("", null);

        }

        /// <summary>
        /// 打印
        /// </summary>
        /// <param name="selectData"></param>
        /// <returns></returns>
        [AuthCode((int)AccessGranularityEnum.ReturnBillsPrint)]
        public JsonResult Print(int type, string selectData, int? terminalId, string terminalName, int? businessUserId, int? deliveryUserId, string billNumber = "", int? wareHouseId = null, string remark = "", DateTime? startTime = null, DateTime? endTime = null, int? districtId = null, bool? auditedStatus = null, bool? sortByAuditedTime = null, bool? showReverse = null, bool? showReturn = null, int? paymentMethodType = null, int? billSourceType = null)
        {
            try
            {

                bool fg = true;
                string errMsg = string.Empty;

                #region 查询打印数据

                IList<ReturnBill> returnBills = new List<ReturnBill>();
                var datas = new List<string>();
                //默认选择
                type = type == 0 ? 1 : type;
                if (type == 1)
                {
                    if (!string.IsNullOrEmpty(selectData))
                    {
                        List<string> ids = selectData.Split(',').ToList();
                        foreach (var id in ids)
                        {
                            ReturnBill returnBill = _returnBillService.GetReturnBillById(curStore.Id, int.Parse(id), true);
                            if (returnBill != null)
                            {
                                returnBills.Add(returnBill);
                            }
                        }
                    }
                }
                else if (type == 2)
                {
                    returnBills = _returnBillService.GetReturnBillList(curStore?.Id ?? 0,
                         curUser.Id,
                        terminalId,
                        terminalName,
                        businessUserId,
                        deliveryUserId,
                        billNumber,
                        wareHouseId,
                        remark,
                        startTime,
                        endTime,
                        districtId,
                        auditedStatus,
                        sortByAuditedTime,
                        showReverse,
                        paymentMethodType,
                        billSourceType,
                        null, false, null, 0);
                }

                #endregion

                #region 修改数据
                if (returnBills != null && returnBills.Count > 0)
                {
                    //using (var scope = new TransactionScope())
                    //{

                    //    scope.Complete();
                    //}
                    #region 修改单据表打印数
                    foreach (var d in returnBills)
                    {
                        d.PrintNum += 1;
                        _returnBillService.UpdateReturnBill(d);
                    }
                    #endregion
                }


                //获取打印模板
                var printTemplates = _printTemplateService.GetAllPrintTemplates(curStore.Id).ToList();
                var content = printTemplates.Where(a => a.BillType == (int)BillTypeEnum.ReturnBill).Select(a => a.Content).FirstOrDefault();

                //获取打印设置
                var pCPrintSetting = _settingService.LoadSetting<PCPrintSetting>(_storeContext.CurrentStore.Id);

                //填充打印数据
                foreach (var d in returnBills)
                {

                    StringBuilder sb = new StringBuilder();
                    sb.Append(content);

                    #region theadid
                    //sb.Replace("@商铺名称", curStore.Name);
                    if (pCPrintSetting != null)
                    {
                        sb.Replace("@商铺名称", string.IsNullOrWhiteSpace(pCPrintSetting.StoreName) ? "&nbsp;" : pCPrintSetting.StoreName);
                    }

                    Terminal terminal = _terminalService.GetTerminalById(curStore.Id, d.TerminalId);
                    if (terminal != null)
                    {
                        sb.Replace("@客户名称", terminal.Name);
                        sb.Replace("@老板姓名", terminal.BossName);
                    }
                    WareHouse wareHouse = _wareHouseService.GetWareHouseById(curStore.Id, d.WareHouseId);
                    if (wareHouse != null)
                    {
                        sb.Replace("@仓库", wareHouse.Name);
                        sb.Replace("@库管员", "***");
                    }
                    User businessUser = _userService.GetUserById(curStore.Id, d.BusinessUserId);
                    if (businessUser != null)
                    {
                        sb.Replace("@业务员", businessUser.UserRealName);
                        sb.Replace("@业务电话", businessUser.MobileNumber);
                    }
                    sb.Replace("@单据编号", d.BillNumber);
                    sb.Replace("@交易日期", d.TransactionDate == null ? "" : ((DateTime)d.TransactionDate).ToString("yyyy/MM/dd HH:mm:ss"));
                    sb.Replace("@打印日期", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                    #endregion

                    #region tbodyid
                    //明细
                    //获取 tbody 中的行
                    int beginTbody = sb.ToString().IndexOf(@"<tbody id=""tbody"">") + @"<tbody id=""tbody"">".Length;
                    if (beginTbody == 17)
                    {
                        beginTbody = sb.ToString().IndexOf(@"<tbody id='tbody'>") + @"<tbody id='tbody'>".Length;
                    }
                    int endTbody = sb.ToString().IndexOf("</tbody>", beginTbody);
                    string tbodytr = sb.ToString()[beginTbody..endTbody];
                    var sumItem1 = 0;
                    var sumItem2 = 0;
                    var sumItem3 = 0;

                    if (d.Items != null && d.Items.Count > 0)
                    {
                        //1.先删除明细第一行
                        sb.Remove(beginTbody, endTbody - beginTbody);
                        int i = 0;
                        var allProducts = _productService.GetProductsByIds(curStore.Id, d.Items.Select(pr => pr.ProductId).Distinct().ToArray());
                        var allOptions = _specificationAttributeService.GetSpecificationAttributeOptionByIds(curStore.Id, allProducts.GetProductBigStrokeSmallUnitIds());
                        foreach (var item in d.Items.OrderByDescending(item => item.Id).ToList())
                        {
                            int index = sb.ToString().IndexOf("</tbody>", beginTbody);
                            i++;
                            StringBuilder sb2 = new StringBuilder();
                            sb2.Append(tbodytr);

                            sb2.Replace("#序号", i.ToString());
                            var product = allProducts.Where(ap => ap.Id == item.ProductId).FirstOrDefault();
                            if (product != null)
                            {
                                sb2.Replace("#商品名称", product.Name);
                                ProductUnitOption productUnitOption = product.GetProductUnit(_specificationAttributeService, _productService);
                                if (item.UnitId == product.SmallUnitId)
                                {
                                    sb2.Replace("#条形码", product.SmallBarCode);
                                    if (productUnitOption != null && productUnitOption.smallOption != null)
                                    {
                                        sb2.Replace("#商品单位", productUnitOption.smallOption.Name);
                                    }

                                }
                                else if (item.UnitId == product.StrokeUnitId)
                                {
                                    sb2.Replace("#条形码", product.StrokeBarCode);
                                    if (productUnitOption != null && productUnitOption.strokOption != null)
                                    {
                                        sb2.Replace("#商品单位", productUnitOption.strokOption.Name);
                                    }
                                }
                                else if (item.UnitId == product.BigUnitId)
                                {
                                    sb2.Replace("#条形码", product.BigBarCode);
                                    if (productUnitOption != null && productUnitOption.bigOption != null)
                                    {
                                        sb2.Replace("#商品单位", productUnitOption.bigOption.Name);
                                    }
                                }

                                sb2.Replace("#单位换算", product.GetProductUnitConversion(allOptions));
                                sb2.Replace("#保质期", (product.ExpirationDays ?? 0).ToString());
                                int conversionQuantity = product.GetConversionQuantity(item.UnitId, _specificationAttributeService, _productService);
                                var qty = Pexts.StockQuantityFormat(conversionQuantity * item.Quantity, 0, product.BigQuantity ?? 0);
                                //var qty = Pexts.StockQuantityFormat(conversionQuantity * item.Quantity, product.StrokeQuantity ?? 0, product.BigQuantity ?? 0);
                                //sb2.Replace("#辅助数量", qty.Item1 + "大" + qty.Item2 + "中" + qty.Item3 + "小");
                                //sb2.Replace("#辅助数量", qty.Item1+ "大" + qty.Item2 + "中" + qty.Item3 + "小");
                                sb2.Replace("#辅助数量", qty.Item1 + productUnitOption.bigOption.Name + qty.Item3 + productUnitOption.smallOption.Name);
                                sumItem1 += qty.Item1;
                                sumItem2 += qty.Item2;
                                sumItem3 += qty.Item3;
                            }
                            sb2.Replace("#数量", item.Quantity.ToString());
                            sb2.Replace("#价格", item.Price.ToString("0.00"));
                            sb2.Replace("#金额", item.Amount.ToString("0.00"));
                            sb2.Replace("#备注", item.Remark);

                            sb.Insert(index, sb2);

                        }
                        sb.Replace("辅助数量:###", sumItem1 + "大" + sumItem2 + "中" + sumItem3 + "小");
                        sb.Replace("数量:###", d.Items.Sum(s => s.Quantity).ToString());
                        sb.Replace("金额:###", d.Items.Sum(s => s.Amount).ToString("0.00"));
                    }
                    #endregion

                    #region tfootid
                    User makeUser = _userService.GetUserById(curStore.Id, d.MakeUserId);
                    if (makeUser != null)
                    {
                        sb.Replace("@制单", makeUser.UserRealName);
                    }
                    sb.Replace("@日期", d.CreatedOnUtc.ToString("yyyy/MM/dd HH:mm:ss"));
                    //sb.Replace("@公司地址", "");
                    if (pCPrintSetting != null)
                    {
                        sb.Replace("@公司地址", pCPrintSetting.Address);
                    }

                    //sb.Replace("@订货电话", "");
                    if (pCPrintSetting != null)
                    {
                        sb.Replace("@订货电话", pCPrintSetting.PlaceOrderTelphone);
                    }

                    sb.Replace("@备注", d.Remark);
                    #endregion

                    datas.Add(sb.ToString());
                }

                if (fg)
                {
                    return Successful("打印成功", datas);
                }
                else
                {
                    return Warning(errMsg);
                }
                #endregion

            }
            catch (Exception ex)
            {
                return Warning(ex.ToString());
            }
        }


        [NonAction]
        private ReturnBillListModel PrepareReturnBillListModel(ReturnBillListModel model)
        {

            //业务员
            model.BusinessUsers = BindUserSelection(_userService.BindUserList, curStore, DCMSDefaults.Salesmans, curUser.Id, true, _userService.IsAdmin(curStore.Id, curUser.Id));

            //仓库
            model.WareHouses = BindWareHouseSelection(_wareHouseService.BindWareHouseList, curStore, WHAEnum.ReturnBill, curUser.Id);

            //送货员
            model.DeliveryUsers = BindUserSelection(_userService.BindUserList, curStore, DCMSDefaults.Delivers, curUser.Id, true, _userService.IsAdmin(curStore.Id, curUser.Id));

            //部门
            model = BindDropDownList<ReturnBillListModel>(model, new Func<int?, int, List<Branch>>(_branchService.BindBranchsByParentId), curStore?.Id ?? 0, 0);

            //片区
            model.Districts = BindDistrictSelection(_districtService.BindDistrictList, curStore);

            return model;
        }

        [NonAction]
        private ReturnBillModel PrepareReturnBillModel(ReturnBillModel model)
        {

            var isAdmin = _userService.IsAdmin(curStore.Id, curUser.Id);

            model.BillTypeEnumId = (int)BillTypeEnum.ReturnBill;

            //业务员
            model.BusinessUsers = BindUserSelection(_userService.BindUserList, curStore, DCMSDefaults.Salesmans, curUser.Id, true, isAdmin);

            //当前用户为业务员时默认绑定
            if (curUser.IsSalesman() && !(model.Id > 0))
            {
                model.BusinessUserId = curUser.Id;
            }
            else
            {
                model.BusinessUserId = model.BusinessUserId == 0 ? null : model.BusinessUserId;
            }

            //仓库
            model.WareHouses = BindWareHouseSelection(_wareHouseService.BindWareHouseList, curStore, WHAEnum.ReturnBill, curUser.Id);

            //送货员
            model.DeliveryUsers = BindUserSelection(_userService.BindUserList, curStore, DCMSDefaults.Delivers, curUser.Id, true, isAdmin);
            model.DeliveryUserId = model.DeliveryUserId == 0 ? null : model.DeliveryUserId;

            //默认售价类型
            model.ReturnDefaultAmounts = BindPricePlanSelection(_productTierPricePlanService.GetAllPricePlan, curStore);
            ////model.DefaultAmountId = (model.DefaultAmountId ?? "");
            ////默认上次售价
            //string lastedPriceDes = CommonHelper.GetEnumDescription(PriceType.LastedPrice);
            //var lastSaleType = model.ReturnDefaultAmounts.Where(sr => sr.Text == lastedPriceDes).FirstOrDefault();
            //model.DefaultAmountId = (model.DefaultAmountId ?? (lastSaleType == null ? "" : lastSaleType.Value));
            //默认
            model.DefaultAmountId = "0_5";
            var companySetting = _settingService.LoadSetting<CompanySetting>(curStore.Id);
            if (companySetting != null)
            {
                if (!string.IsNullOrEmpty(companySetting.DefaultPricePlan))
                {
                    //分析配置 格式："{PricesPlanId}_{PriceTypeId}"
                    var settingDefault = model.ReturnDefaultAmounts?.Where(s => s.Value.EndsWith(companySetting.DefaultPricePlan.ToString())).FirstOrDefault();
                    //这里取默认（不选择时），否启手动下拉选择
                    model.DefaultAmountId = settingDefault?.Value; //如：0_0
                }
            }

            return model;
        }

    }
}